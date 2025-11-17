using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace QuickerActionManage.ViewModel
{
    /// <summary>
    /// Action group model
    /// Structure: "groupId": { name: "" }
    /// Action-to-group mapping is stored separately as actionId => groupId
    /// </summary>
    public partial class ActionGroup : ObservableObject
    {
        /// <summary>
        /// Group ID (used as key in storage)
        /// </summary>
        [ObservableProperty]
        public partial string GroupId { get; set; } = string.Empty;

        /// <summary>
        /// Group display name
        /// </summary>
        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;

        /// <summary>
        /// Special constant for "All" group ID
        /// </summary>
        public const string AllGroupId = "__ALL__";

        /// <summary>
        /// Special constant for "All" group name
        /// </summary>
        public const string AllGroupName = "全部";

        /// <summary>
        /// Check if this is the special "All" group
        /// </summary>
        [JsonIgnore]
        public bool IsAllGroup => GroupId == AllGroupId;

        /// <summary>
        /// Check if this group can be edited (not "All" group)
        /// </summary>
        [JsonIgnore]
        public bool CanEdit => !IsAllGroup;

        public ActionGroup() { }

        public ActionGroup(string groupId, string name)
        {
            GroupId = groupId;
            Name = name;
        }

        /// <summary>
        /// Create the special "All" group (virtual, not stored)
        /// </summary>
        public static ActionGroup CreateAllGroup()
        {
            return new ActionGroup(AllGroupId, AllGroupName);
        }
    }
}

