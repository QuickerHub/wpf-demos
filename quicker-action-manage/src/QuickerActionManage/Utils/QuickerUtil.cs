using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Quicker.Common;
using Quicker.Domain;
using Quicker.Domain.Actions;
using Quicker.Domain.Actions.X;
using Quicker.Domain.Actions.X.Storage;
using Quicker.Domain.Actions.Runtime;
using Quicker.Domain.Services;
using Quicker.Public.Actions;
using Quicker.Utilities;
using Quicker.Utilities._3rd;
using Quicker.Utilities.UI.Behaviors;

namespace QuickerActionManage.Utils
{
    public static class QuickerUtil
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(QuickerUtil));

        public static ActionEditMgr ActionEditMgr
        {
            get
            {
                if (!IsInQuicker) return null;
                try
                {
                    var result = (ActionEditMgr)typeof(AppState).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                                                           .First(x => x.ReturnType == typeof(ActionEditMgr))
                                                           .Invoke(null, null);
                    if (result != null) return result;
                }
                catch { }
                return null;
            }
        }

        public static RecentActionMgr RecentActionMgr
        {
            get
            {
                if (!IsInQuicker) return null;
                try
                {
                    var result = (RecentActionMgr)typeof(AppState).GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                                                             .First(x => x.FieldType == typeof(RecentActionMgr))
                                                             .GetValue(AppState.AppServer);
                    if (result != null) return result;
                }
                catch { }
                return null;
            }
        }

        public static bool IsInQuicker { get; }
        static QuickerUtil()
        {
            IsInQuicker = Assembly.GetEntryAssembly()?.GetName().Name == "Quicker";
        }

        public static bool CheckIsInQuicker() => Assembly.GetEntryAssembly()?.GetName().Name == "Quicker";

        public static ActionProfile GetActioinProfileById(string actionId)
        {
            if (CheckIsInQuicker())
            {
                return AppState.DataService.GetActionById(actionId).profile;
            }
            else
            {
                return new ActionProfile();
            }
        }

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

        public static IEnumerable<ActionItem> GetAllActionItems()
        {
            if (CheckIsInQuicker())
            {
                return AppState.DataService.GetAllActionItems();
            }
            else
            {
                // Use fixed IDs for debug mode so groups can match actions correctly
                return new List<ActionItem>()
                {
                    new(){Id = "debug-action-1", Title = "aaaaaaaa",LastEditTimeUtc=DateTime.UtcNow,ShareTimeUtc=DateTime.UtcNow},
                    new(){Id = "debug-action-2", Title = "bbbbbbbb",LastEditTimeUtc = DateTime.UtcNow},
                    new(){Id = "debug-action-3", Title ="cccccccc"},
                };
            }
        }

        public static SmartCollection<SubProgram> GetGlobalSubprograms()
        {
            if (CheckIsInQuicker())
            {
                return AppState.DataService.GlobalSubPrograms;
            }
            else
            {
                return new SmartCollection<SubProgram>()
                {
                    new(){Name="aaa",LastEditTimeUtc=DateTime.UtcNow,ShareTimeUtc=DateTime.UtcNow,Id ="11111" },
                    new(){Name="ccc",Id = "222222"},
                    new(){Name="bbb", LastEditTimeUtc=DateTime.Now,Id = "3333333"},
                };
            }
        }

        public static void EditAction(string id)
        {
            AppState.AppServer.EditActionById(id);
        }

        public static void RunActionAndRecord(string id, string cmds)
        {
            AppState.AppServer.ExecuteActionByIdOrName(id, null, enableDebugging: false, false, true, cmds, ActionTrigger.FloatButton);
            RecordAction(id, cmds);
        }

        public static void RecordAction(string id, string param)
        {
            RecentActionMgr.RecordLastAction(id, param);
        }

        public static void RunAction(string id, string cmds, bool wait)
        {
            AppState.AppServer.ExecuteActionByIdOrName(id, null, enableDebugging: false, wait, true, cmds, ActionTrigger.FloatButton);
        }

        public static void DebugAction(string id, string cmds, bool wait)
        {
            AppState.AppServer.ExecuteActionByIdOrName(id, null, enableDebugging: true, wait, true, cmds, ActionTrigger.FloatButton);
        }

        public static void CreateOrEditGlobalSubprogram(SubProgram sub)
        {
            ActionEditMgr.CreateOrEditGlobalSubProgram(sub);
        }

        public static void ShareSubprogram(SubProgram sub, Window owner, bool isGlobal)
        {
            ActionEditMgr.ShareSubProgram(sub, owner, isGlobal);
        }

        public static void CreateActionMenus(ContextMenu menu, string actionId, Window owner, bool showDelete = false)
        {
            if (!IsInQuicker)
                return;
            (ActionItem action, ActionProfile profile) = AppState.DataService.GetActionById(actionId);
            ActionEditMgr.BuildMenuForActionButton(menu, action, profile, 0, 0, owner, ActionTrigger.NA, showDelete);
        }

        public static async Task<bool> DeleteAction(string id)
        {
            if (!IsInQuicker)
                return false;
            (ActionItem action, ActionProfile profile) = AppState.DataService.GetActionById(id);
            return await ActionEditMgr.DeleteAction(profile, action, true, false);
        }

        public static async Task<bool> DeleteAction(IEnumerable<string> ids)
        {
            bool success = false;
            foreach (string id in ids)
            {
                (ActionItem action, ActionProfile profile) = AppState.DataService.GetActionById(id);
                success = await ActionEditMgr.DeleteAction(profile, action, false, false);
            }
            return success;
        }

        /// <summary>
        /// Set window can use Quicker
        /// </summary>
        public static void SetCanUseQuicker(Window window)
        {
            if (window == null) return;
            var handle = new WindowInteropHelper(window).Handle;
            if (handle != IntPtr.Zero)
            {
                BlockQuickerHWndBehavior.HWnds.TryRemove(handle, out _);
            }
        }

        /// <summary>
        /// Set window can use Quicker by handle
        /// </summary>
        public static void SetCanUseQuicker(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
            {
                BlockQuickerHWndBehavior.HWnds.TryRemove(handle, out _);
            }
        }
    }
}

