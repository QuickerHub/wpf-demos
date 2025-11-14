using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Quicker.Common;
using Quicker.Domain;
using Quicker.Domain.Actions;
using Quicker.Domain.Actions.X;
using Quicker.Domain.Actions.X.Storage;
using Quicker.Public.Actions;
using Quicker.Utilities;

namespace QuickerActionManage.Utils
{
    /// <summary>
    /// Quicker utility wrapper class
    /// </summary>
    public static class QuickerUtil
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(QuickerUtil));

        public static bool IsInQuicker { get; }

        static QuickerUtil()
        {
            IsInQuicker = Assembly.GetEntryAssembly()?.GetName().Name == "Quicker";
        }

        public static bool CheckIsInQuicker() => IsInQuicker;

        /// <summary>
        /// Get action profile by ID
        /// </summary>
        public static ActionProfile GetActioinProfileById(string actionId)
        {
            if (CheckIsInQuicker())
            {
                try
                {
                    return AppState.DataService.GetActionById(actionId).profile;
                }
                catch
                {
                    return new ActionProfile();
                }
            }
            else
            {
                return new ActionProfile();
            }
        }

        /// <summary>
        /// Get all action items
        /// </summary>
        public static IEnumerable<ActionItem> GetAllActionItems()
        {
            if (CheckIsInQuicker())
            {
                try
                {
                    return AppState.DataService.GetAllActionItems();
                }
                catch
                {
                    return Enumerable.Empty<ActionItem>();
                }
            }
            return Enumerable.Empty<ActionItem>();
        }

        /// <summary>
        /// Get global subprograms
        /// </summary>
        public static Quicker.Utilities._3rd.SmartCollection<SubProgram> GetGlobalSubprograms()
        {
            if (CheckIsInQuicker())
            {
                try
                {
                    return AppState.DataService.GlobalSubPrograms;
                }
                catch
                {
                    return new Quicker.Utilities._3rd.SmartCollection<SubProgram>();
                }
            }
            return new Quicker.Utilities._3rd.SmartCollection<SubProgram>();
        }

        /// <summary>
        /// Get ActionEditMgr using reflection
        /// </summary>
        public static object? ActionEditMgr
        {
            get
            {
                if (!IsInQuicker) return null;
                try
                {
                    var method = typeof(AppState).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                                                           .FirstOrDefault(x => x.ReturnType.Name == "ActionEditMgr");
                    return method?.Invoke(null, null);
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Get ActionRunMgr using reflection
        /// </summary>
        public static object? ActionRunMgr
        {
            get
            {
                if (!IsInQuicker) return null;
                try
                {
                    var method = typeof(AppState).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                                                           .FirstOrDefault(x => x.ReturnType.Name == "ActionRunMgr");
                    return method?.Invoke(null, null);
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Get action by ID
        /// </summary>
        public static ActionItem? GetActionById(string? actionId)
        {
            if (IsInQuicker)
            {
                try
                {
                    return AppState.DataService.GetActionById(actionId).action;
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Edit action
        /// </summary>
        public static void EditAction(string actionId)
        {
            if (CheckIsInQuicker())
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ActionEditMgr?.GetType().GetMethod("EditAction", new[] { typeof(string) })?.Invoke(ActionEditMgr, new object[] { actionId });
                    });
                }
                catch (Exception ex)
                {
                    _log.Error($"Failed to edit action: {actionId}", ex);
                }
            }
        }

        /// <summary>
        /// Run action and record
        /// </summary>
        public static void RunActionAndRecord(string actionId, string param = "")
        {
            if (CheckIsInQuicker())
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ActionRunMgr?.GetType().GetMethod("RunAction", new[] { typeof(string), typeof(string) })?.Invoke(ActionRunMgr, new object[] { actionId, param });
                    });
                }
                catch (Exception ex)
                {
                    _log.Error($"Failed to run action: {actionId}", ex);
                }
            }
        }

        /// <summary>
        /// Debug action
        /// </summary>
        public static void DebugAction(string actionId, string param = "", bool breakOnStart = false)
        {
            if (CheckIsInQuicker())
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ActionEditMgr?.GetType().GetMethod("DebugAction", new[] { typeof(string), typeof(string), typeof(bool) })?.Invoke(ActionEditMgr, new object[] { actionId, param, breakOnStart });
                    });
                }
                catch (Exception ex)
                {
                    _log.Error($"Failed to debug action: {actionId}", ex);
                }
            }
        }

        /// <summary>
        /// Create or edit global subprogram
        /// </summary>
        public static void CreateOrEditGlobalSubprogram(SubProgram subprogram)
        {
            if (CheckIsInQuicker())
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ActionEditMgr?.GetType().GetMethod("CreateOrEditGlobalSubProgram", new[] { typeof(SubProgram) })?.Invoke(ActionEditMgr, new object[] { subprogram });
                    });
                }
                catch (Exception ex)
                {
                    _log.Error($"Failed to edit global subprogram: {subprogram.Id}", ex);
                }
            }
        }

        /// <summary>
        /// Share subprogram
        /// </summary>
        public static void ShareSubprogram(SubProgram subprogram, Window? owner = null, bool showDialog = true)
        {
            if (CheckIsInQuicker())
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ActionEditMgr?.GetType().GetMethod("ShareSubProgram", new[] { typeof(SubProgram), typeof(Window), typeof(bool) })?.Invoke(ActionEditMgr, new object[] { subprogram, owner, showDialog });
                    });
                }
                catch (Exception ex)
                {
                    _log.Error($"Failed to share subprogram: {subprogram.Id}", ex);
                }
            }
        }

        /// <summary>
        /// Create action menus
        /// </summary>
        public static void CreateActionMenus(System.Windows.Controls.ContextMenu menu, string actionId, Window owner, bool showDelete = false)
        {
            if (CheckIsInQuicker())
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var (action, profile) = AppState.DataService.GetActionById(actionId);
                        var actionTriggerType = typeof(AppState).Assembly.GetType("Quicker.Domain.Actions.Runtime.ActionTrigger");
                        var naValue = Enum.Parse(actionTriggerType, "NA");
                        ActionEditMgr?.GetType().GetMethod("BuildMenuForActionButton", new[] { typeof(System.Windows.Controls.ContextMenu), typeof(ActionItem), typeof(ActionProfile), typeof(int), typeof(int), typeof(Window), actionTriggerType, typeof(bool) })
                            ?.Invoke(ActionEditMgr, new object[] { menu, action, profile, 0, 0, owner, naValue, showDelete });
                    });
                }
                catch (Exception ex)
                {
                    _log.Error($"Failed to create action menus: {actionId}", ex);
                }
            }
        }

        /// <summary>
        /// Delete action
        /// </summary>
        public static async System.Threading.Tasks.Task<bool> DeleteAction(string id)
        {
            if (CheckIsInQuicker())
            {
                try
                {
                    var task = Application.Current.Dispatcher.Invoke<System.Threading.Tasks.Task<bool>>(() =>
                    {
                        var (action, profile) = AppState.DataService.GetActionById(id);
                        var deleteMethod = ActionEditMgr?.GetType().GetMethod("DeleteAction", new[] { typeof(ActionProfile), typeof(ActionItem), typeof(bool), typeof(bool) });
                        if (deleteMethod != null)
                        {
                            return (System.Threading.Tasks.Task<bool>)deleteMethod.Invoke(ActionEditMgr, new object[] { profile, action, true, false });
                        }
                        return System.Threading.Tasks.Task.FromResult(false);
                    });
                    return await task;
                }
                catch (Exception ex)
                {
                    _log.Error($"Failed to delete action: {id}", ex);
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Delete multiple actions
        /// </summary>
        public static async System.Threading.Tasks.Task<bool> DeleteAction(IEnumerable<string> ids)
        {
            if (CheckIsInQuicker())
            {
                try
                {
                    bool success = false;
                    var deleteMethod = ActionEditMgr?.GetType().GetMethod("DeleteAction", new[] { typeof(ActionProfile), typeof(ActionItem), typeof(bool), typeof(bool) });
                    if (deleteMethod != null)
                    {
                        foreach (string id in ids)
                        {
                            var (action, profile) = AppState.DataService.GetActionById(id);
                            var task = (System.Threading.Tasks.Task<bool>)deleteMethod.Invoke(ActionEditMgr, new object[] { profile, action, false, false });
                            success = await task;
                        }
                    }
                    return success;
                }
                catch (Exception ex)
                {
                    _log.Error($"Failed to delete actions", ex);
                    return false;
                }
            }
            return false;
        }
    }
}

