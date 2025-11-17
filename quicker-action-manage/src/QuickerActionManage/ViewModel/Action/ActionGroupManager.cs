using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using QuickerActionManage.Interfaces;
using QuickerActionManage.State;
using QuickerActionManage.Utils;

namespace QuickerActionManage.ViewModel
{
    /// <summary>
    /// Manages action groups
    /// Uses actionId => groupId mapping instead of storing actionIds in groups
    /// </summary>
    public class ActionGroupManager : IPostInit
    {
        private readonly GlobalStateWriter _stateWriter;
        private readonly string GroupsKey;
        private readonly string ActionToGroupKey;

        /// <summary>
        /// Mapping from actionId to groupId
        /// </summary>
        private Dictionary<string, string> _actionToGroup = new();

        /// <summary>
        /// All groups including the special "All" group
        /// </summary>
        public FullyObservableCollection<ActionGroup> Groups { get; } = new();

        /// <summary>
        /// The special "All" group (virtual, not stored)
        /// </summary>
        public ActionGroup AllGroup { get; }

        public ActionGroupManager()
        {
            _stateWriter = new GlobalStateWriter(typeof(ActionGroupManager).FullName);
            // Use different key for debug mode to avoid mixing test data with real data
            var keyPrefix = QuickerUtil.CheckIsInQuicker() ? "ActionGroups" : "ActionGroups_Debug";
            GroupsKey = keyPrefix;
            ActionToGroupKey = $"{keyPrefix}_ActionToGroup";
            AllGroup = ActionGroup.CreateAllGroup();
            Groups.Add(AllGroup);
            
            // Load groups and mappings without subscribing to events
            LoadGroups();
            LoadActionToGroup();
        }

        /// <summary>
        /// Post-initialization: Subscribe to events for auto-save
        /// </summary>
        public void PostInit()
        {
            // Subscribe to changes to auto-save
            Groups.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (ActionGroup group in e.NewItems)
                    {
                        if (!group.IsAllGroup)
                        {
                            group.PropertyChanged += (sender, args) => SaveGroups();
                        }
                    }
                }
                SaveGroups();
            };
            
            Groups.ItemPropertyChanged += (s, e) =>
            {
                if (e.CollectionIndex >= 0 && e.CollectionIndex < Groups.Count)
                {
                    var group = Groups[e.CollectionIndex];
                    if (!group.IsAllGroup)
                    {
                        SaveGroups();
                    }
                }
            };

            // Subscribe to existing groups' events
            foreach (var group in Groups)
            {
                if (!group.IsAllGroup)
                {
                    group.PropertyChanged += (sender, args) => SaveGroups();
                }
            }
        }

        /// <summary>
        /// Load groups from storage
        /// </summary>
        private void LoadGroups()
        {
            try
            {
                var data = _stateWriter.Read(GroupsKey) as string;
                System.Diagnostics.Debug.WriteLine($"Loading groups data: {data ?? "null"}");
                if (string.IsNullOrEmpty(data))
                {
                    return;
                }

                var groupsDict = JsonConvert.DeserializeObject<Dictionary<string, ActionGroupData>>(data);
                if (groupsDict == null)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to deserialize groups data");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"Loaded {groupsDict.Count} groups from storage");

                // Clear existing groups (except "All")
                var groupsToRemove = Groups.Where(g => !g.IsAllGroup).ToList();
                foreach (var group in groupsToRemove)
                {
                    Groups.Remove(group);
                }

                // Load groups from storage
                foreach (var kvp in groupsDict)
                {
                    var groupData = kvp.Value;
                    var group = new ActionGroup(kvp.Key, groupData.Name);
                    
                    // Event subscriptions will be set up in PostInit
                    
                    Groups.Add(group);
                }
            }
            catch (Exception ex)
            {
                // Log error if logger is available
                System.Diagnostics.Debug.WriteLine($"Failed to load groups: {ex.Message}");
            }
        }

        /// <summary>
        /// Load action-to-group mapping from storage
        /// </summary>
        private void LoadActionToGroup()
        {
            try
            {
                var data = _stateWriter.Read(ActionToGroupKey) as string;
                System.Diagnostics.Debug.WriteLine($"Loading action-to-group mapping: {data ?? "null"}");
                if (string.IsNullOrEmpty(data))
                {
                    _actionToGroup = new Dictionary<string, string>();
                    return;
                }

                var mapping = JsonConvert.DeserializeObject<Dictionary<string, string>>(data);
                if (mapping == null)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to deserialize action-to-group mapping");
                    _actionToGroup = new Dictionary<string, string>();
                    return;
                }
                
                _actionToGroup = mapping;
                System.Diagnostics.Debug.WriteLine($"Loaded {_actionToGroup.Count} action-to-group mappings");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load action-to-group mapping: {ex.Message}");
                _actionToGroup = new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Save groups to storage (excluding "All" group)
        /// </summary>
        private void SaveGroups()
        {
            try
            {
                var groupsDict = new Dictionary<string, ActionGroupData>();
                
                foreach (var group in Groups)
                {
                    if (!group.IsAllGroup)
                    {
                        groupsDict[group.GroupId] = new ActionGroupData
                        {
                            Name = group.Name
                        };
                    }
                }

                var json = JsonConvert.SerializeObject(groupsDict);
                System.Diagnostics.Debug.WriteLine($"Saving groups: {json}");
                _stateWriter.Write(GroupsKey, json);
                
                // Also save action-to-group mapping
                SaveActionToGroup();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save groups: {ex.Message}");
            }
        }

        /// <summary>
        /// Save action-to-group mapping to storage
        /// </summary>
        private void SaveActionToGroup()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_actionToGroup);
                System.Diagnostics.Debug.WriteLine($"Saving action-to-group mapping: {json}");
                _stateWriter.Write(ActionToGroupKey, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save action-to-group mapping: {ex.Message}");
            }
        }

        /// <summary>
        /// Add a new group
        /// </summary>
        public ActionGroup AddGroup(string name)
        {
            var groupId = Guid.NewGuid().ToString();
            var group = new ActionGroup(groupId, name);
            
            // Subscribe to property changes for auto-save
            group.PropertyChanged += (sender, args) =>
            {
                if (!group.IsAllGroup)
                {
                    SaveGroups();
                }
            };
            
            Groups.Add(group);
            return group;
        }

        /// <summary>
        /// Remove a group
        /// Also removes all action-to-group mappings for this group
        /// </summary>
        public void RemoveGroup(ActionGroup group)
        {
            if (group.IsAllGroup)
            {
                throw new InvalidOperationException("Cannot remove the 'All' group");
            }

            // Remove all action-to-group mappings for this group
            var actionIdsToRemove = _actionToGroup.Where(kvp => kvp.Value == group.GroupId)
                                                   .Select(kvp => kvp.Key)
                                                   .ToList();
            foreach (var actionId in actionIdsToRemove)
            {
                _actionToGroup.Remove(actionId);
            }

            Groups.Remove(group);
            
            // Save the updated mapping
            SaveActionToGroup();
        }

        /// <summary>
        /// Get group by ID
        /// </summary>
        public ActionGroup? GetGroupById(string groupId)
        {
            return Groups.FirstOrDefault(g => g.GroupId == groupId);
        }

        /// <summary>
        /// Get action IDs for a specific group
        /// If group is "All", returns null (meaning all actions)
        /// </summary>
        public HashSet<string>? GetActionIdsForGroup(ActionGroup? group)
        {
            if (group == null || group.IsAllGroup)
            {
                return null; // null means all actions
            }

            // Get all actionIds that map to this group
            return new HashSet<string>(
                _actionToGroup.Where(kvp => kvp.Value == group.GroupId)
                             .Select(kvp => kvp.Key));
        }

        /// <summary>
        /// Get the group that contains the specified action
        /// </summary>
        public ActionGroup? GetGroupForAction(string actionId)
        {
            if (!_actionToGroup.TryGetValue(actionId, out var groupId))
            {
                return null;
            }

            return GetGroupById(groupId);
        }

        /// <summary>
        /// Add action to group
        /// If action is already in another group, it will be moved to the new group
        /// </summary>
        public void AddActionToGroup(ActionGroup group, string actionId)
        {
            if (group.IsAllGroup)
            {
                throw new InvalidOperationException("Cannot modify the 'All' group");
            }

            // Add or update the mapping
            _actionToGroup[actionId] = group.GroupId;
            SaveActionToGroup();
        }

        /// <summary>
        /// Remove action from group
        /// </summary>
        public void RemoveActionFromGroup(ActionGroup group, string actionId)
        {
            if (group.IsAllGroup)
            {
                throw new InvalidOperationException("Cannot modify the 'All' group");
            }

            // Only remove if the action is actually in this group
            if (_actionToGroup.TryGetValue(actionId, out var currentGroupId) && currentGroupId == group.GroupId)
            {
                _actionToGroup.Remove(actionId);
                SaveActionToGroup();
            }
        }

        /// <summary>
        /// Internal data structure for storage
        /// </summary>
        private class ActionGroupData
        {
            public string Name { get; set; } = string.Empty;
        }
    }
}

