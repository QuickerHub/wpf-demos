using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using log4net;
using Quicker.Common;
using Quicker.Domain;
using Quicker.Domain.Actions;
using Quicker.Domain.Actions.Runtime;
using Quicker.Domain.Actions.X;
using Quicker.Domain.Actions.X.Storage;
using Quicker.Domain.Services;
using Quicker.Public.Actions;
using Quicker.Public.Interfaces;
using Quicker.Utilities;
using Quicker.View;
using Quicker.View.X;
using Quicker.View.X.Controls;

namespace Alinko.Quicker.Services
{
    /// <summary>
    /// Service for action editing operations
    /// </summary>
    public class ActionEditService
    {
        private readonly ActionEditMgr _actionEditMgr;
        private readonly ILog _log;

        /// <summary>
        /// Initialize ActionEditService and get ActionEditMgr instance
        /// </summary>
        public ActionEditService()
        {
            _log = LogManager.GetLogger(typeof(ActionEditService));
            _actionEditMgr = GetActionEditMgr();
            if (_actionEditMgr == null)
            {
                throw new InvalidOperationException("Failed to get ActionEditMgr. Make sure you are running in Quicker environment.");
            }
        }

        /// <summary>
        /// Get ActionEditMgr instance using reflection
        /// </summary>
        private ActionEditMgr? GetActionEditMgr()
        {
            if (!IsInQuicker()) return null;

            try
            {
                var result = (ActionEditMgr)typeof(AppState).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                                                           .First(x => x.ReturnType == typeof(ActionEditMgr))
                                                           .Invoke(null, null);
                if (result != null) return result;
            }
            catch (Exception ex)
            {
                _log.Warn("Failed to get ActionEditMgr", ex);
            }

            return null;
        }

        /// <summary>
        /// Check if running in Quicker environment
        /// </summary>
        private bool IsInQuicker()
        {
            return Assembly.GetEntryAssembly()?.GetName().Name == "Quicker";
        }

        /// <summary>
        /// Update shared action with change log
        /// </summary>
        /// <param name="id">Action ID</param>
        /// <param name="changeLog">Change log message</param>
        /// <returns>Result message</returns>
        public async Task<string> UpdateActionAsync(string id, string changeLog)
        {
            var (isSuccess, message) = await _actionEditMgr.UpdateSharedActionAsync(id, changeLog);
            return message;
        }

        /// <summary>
        /// Update multiple shared actions with change log
        /// </summary>
        /// <param name="ids">Action IDs</param>
        /// <param name="changeLog">Change log message</param>
        /// <returns>List of error messages</returns>
        public async Task<List<string>> UpdateActionsAsync(IEnumerable<string> ids, string changeLog)
        {
            var errors = new List<string>();

            foreach (var id in ids)
            {
                var (isSuccess, message) = await _actionEditMgr.UpdateSharedActionAsync(id, changeLog);
                var action = GetActionById(id);
                message = $"{action?.Title}; {message}";

                if (isSuccess)
                {
                    AppHelper.ShowSuccess(message);
                    _log.Info(message);
                }
                else
                {
                    errors.Add(message);
                    AppHelper.ShowWarning(message);
                    _log.Warn(message);
                }

                await Task.Delay(200);
            }

            return errors;
        }

        /// <summary>
        /// Edit global subprogram by ID or name
        /// Must be called on UI thread
        /// </summary>
        /// <param name="idOrName">Subprogram ID or name</param>
        public void EditGlobalSubProgramById(string idOrName)
        {
            var sub = AppState.DataService.GetGlobalSubProgram(idOrName);
            _actionEditMgr.CreateOrEditGlobalSubProgram(sub);
        }

        /// <summary>
        /// Edit variable version in global subprogram
        /// </summary>
        /// <param name="idOrName">Subprogram ID or name</param>
        /// <param name="version">Version number</param>
        public async Task EditVarVersionAsync(string idOrName, double version)
        {
            await EditVarVersionAsync(idOrName, version.ToString());
        }


        public static nint GetForeGroundWindow()
        {
            return Windows.Win32.PInvoke.GetForegroundWindow().Value;
        }


        public static WType? GetWindow<WType>(nint handle) where WType : class => HwndSource.FromHwnd(handle)?.RootVisual as WType;
        public static Window? GetWindow(nint handle) => GetWindow<Window>(handle);
        public static Window? GetWindow() => GetWindow<Window>(GetForeGroundWindow());

        /// <summary>
        /// Edit variable version in global subprogram
        /// </summary>
        /// <param name="idOrName">Subprogram ID or name</param>
        /// <param name="version">Version string</param>
        public async Task EditVarVersionAsync(string idOrName, string version)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                EditGlobalSubProgramById(idOrName);
            });

            Window? win = null;
            for (int i = 0; i < 30; i++)
            {
                win = Application.Current.Dispatcher.Invoke(WindowHelper.GetWindow);
                if (win is ActionDesignerWindow)
                    break;
                await Task.Delay(100);
            }

            if (win is ActionDesignerWindow designer)
            {
                await Task.Delay(200);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    designer.DoActionOnLoaded(() =>
                    {
                        var varVersion = designer.VariableList.Cast<ActionVariable>().FirstOrDefault(x => x.Key == "version");
                        if (varVersion == null)
                            return;

                        var lastVersion = varVersion.DefaultValue;
                        varVersion.DefaultValue = version;
                        AppHelper.ShowSuccess($"版本变更 {lastVersion} => {version} ");
                        (designer.FindName("BtnSave") as Button)?.TriggerClick();
                    });
                });
            }
        }

        /// <summary>
        /// Access current action definition
        /// </summary>
        /// <param name="option">Option: "1" get action, "2" set action, "3" set from clipboard</param>
        /// <param name="strAction">Action data string (for option "2")</param>
        /// <returns>Action data string (for option "1")</returns>
        public string? CurrentActionAccess(string option, string strAction)
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                var editor = new ActionEditor();
                switch (option)
                {
                    case "1": return editor.GetAction().ToJson();
                    case "2": editor.SetAction(strAction); break;
                    case "3": editor.SetFromClipboard(); break;
                }
                return null;
            });
        }

        /// <summary>
        /// Create or edit global subprogram
        /// </summary>
        /// <param name="sub">Subprogram instance</param>
        public void CreateOrEditGlobalSubprogram(SubProgram sub)
        {
            _actionEditMgr.CreateOrEditGlobalSubProgram(sub);
        }

        /// <summary>
        /// Share subprogram
        /// </summary>
        /// <param name="sub">Subprogram instance</param>
        /// <param name="owner">Owner window</param>
        /// <param name="isGlobal">Is global subprogram</param>
        public void ShareSubprogram(SubProgram sub, Window owner, bool isGlobal)
        {
            _actionEditMgr.ShareSubProgram(sub, owner, isGlobal);
        }

        /// <summary>
        /// Update action (share action)
        /// </summary>
        /// <param name="id">Action ID</param>
        public void UpdateAction(string id)
        {
            var (action, profile) = AppState.DataService.GetActionById(id);
            _actionEditMgr.ShareAction(action, profile, null);
        }

        /// <summary>
        /// Create action context menu
        /// </summary>
        /// <param name="menu">Context menu</param>
        /// <param name="actionId">Action ID</param>
        /// <param name="owner">Owner window</param>
        /// <param name="showDelete">Show delete option</param>
        public void CreateActionMenus(ContextMenu menu, string actionId, Window owner, bool showDelete = false)
        {
            var (action, profile) = AppState.DataService.GetActionById(actionId);
            _actionEditMgr.BuildMenuForActionButton(menu, action, profile, 0, 0, owner, ActionTrigger.NA, showDelete);
        }

        /// <summary>
        /// Get action menu items
        /// </summary>
        /// <param name="actionId">Action ID</param>
        /// <param name="showDelete">Show delete option</param>
        /// <returns>List of menu controls</returns>
        public List<Control> GetActionMenus(string actionId, bool showDelete = false)
        {
            var menu = new ContextMenu();
            var (action, profile) = AppState.DataService.GetActionById(actionId);
            _actionEditMgr.BuildMenuForActionButton(menu, action, profile, 0, 0, GetMainWindow(), ActionTrigger.NA, showDelete: false);
            var items = menu.Items.OfType<Control>().ToList();
            menu.Items.Clear();
            return items;
        }

        /// <summary>
        /// Save action (TODO: implementation needed)
        /// </summary>
        public void SaveAction()
        {
            // TODO: implementation needed
        }

        /// <summary>
        /// Delete action
        /// </summary>
        /// <param name="id">Action ID</param>
        /// <returns>Success status</returns>
        public async Task<bool> DeleteActionAsync(string id)
        {
            var (action, profile) = AppState.DataService.GetActionById(id);
            return await _actionEditMgr.DeleteAction(profile, action, true, false);
        }

        /// <summary>
        /// Delete multiple actions
        /// </summary>
        /// <param name="ids">Action IDs</param>
        /// <returns>Success status</returns>
        public async Task<bool> DeleteActionsAsync(IEnumerable<string> ids)
        {
            bool success = false;
            foreach (string id in ids)
            {
                var (action, profile) = AppState.DataService.GetActionById(id);
                success = await _actionEditMgr.DeleteAction(profile, action, false, false);
            }
            return success;
        }

        /// <summary>
        /// Share action
        /// </summary>
        /// <param name="id">Action ID</param>
        /// <param name="owner">Owner window</param>
        public void ShareAction(string id, Window owner)
        {
            var (action, profile) = AppState.DataService.GetActionById(id);
            _actionEditMgr?.ShareAction(action, profile, owner);
        }

        /// <summary>
        /// Edit action by ID
        /// </summary>
        /// <param name="id">Action ID</param>
        public void EditAction(string id)
        {
            AppState.AppServer.EditActionById(id);
        }

        /// <summary>
        /// Vote for action
        /// </summary>
        /// <param name="actionId">Action ID</param>
        /// <param name="isTemplateId">Is template ID</param>
        public async Task VoteActionAsync(string actionId, bool isTemplateId = false)
        {
            var templateId = GetTemplateId(actionId);
            var methods = typeof(ActionEditMgr).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                               .Where(x => x.ReturnType == typeof(Task))
                                               .Where(x => x.GetParameters().Length == 1);
            var method = methods.First();
            var actionItem = new ActionItem() { TemplateId = isTemplateId ? actionId : templateId };

            await (Task)method.Invoke(_actionEditMgr, new[] { actionItem });
        }

        /// <summary>
        /// Get action by ID
        /// </summary>
        private ActionItem? GetActionById(string? actionId)
        {
            if (IsInQuicker())
            {
                try
                {
                    return AppState.DataService.GetActionById(actionId).action;
                }
                catch { }
            }
            return null;
        }

        /// <summary>
        /// Get template ID from action ID
        /// </summary>
        private string GetTemplateId(string actionId)
        {
            var action = GetActionById(actionId);
            if (action == null)
            {
                return "";
            }
            return action.TemplateId ?? action.SharedActionId ?? "";
        }

        /// <summary>
        /// Get main window
        /// </summary>
        private PopupWindow GetMainWindow()
        {
            return WindowHelper.GetWindow(AppState.MainWinHandle) as PopupWindow ?? throw new ArgumentNullException();
        }
    }
}

