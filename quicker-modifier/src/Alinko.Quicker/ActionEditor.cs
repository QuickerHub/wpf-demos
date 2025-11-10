using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using Quicker.Common;
using Quicker.Domain.Actions.X;
using Quicker.Public.Extensions;
using Quicker.View.X;

namespace Alinko.Quicker;

public class ActionEditor
{
    private readonly ActionDesignerWindow _desiger;

    public ActionEditor() : this(WindowHelper.GetForeGroundWindow()) { }

    public ActionEditor(IntPtr handle)
    {
        var win = WindowHelper.GetWindow<ActionDesignerWindow>(handle);
        if (win is not ActionDesignerWindow designer)
        {
            throw new Exception("不是 ActionDesignerWindow");
        }
        this._desiger = designer;
    }

    public XAction GetAction() => _desiger.Action;
    public void SetAction(string action)
    {
        _desiger.Action = action.JsonToObject<XAction>();
        Update();
    }
    public void SetFromClipboard()
    {
        if (Clipboard.ContainsData("quicker-action-item"))
        {
            if (Clipboard.GetData("quicker-action-item") is ActionItem actionItem)
            {
                SetAction(actionItem.Data);
            }
        }
    }

    public void Update() => _desiger.CallMethod(MethodName);
    public static string MethodName { get; } = GetMethodName();
    internal static string GetMethodName()
    {
        var type = typeof(ActionDesignerWindow);
        List<string> methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Select(x => x.Name).ToList();
        var index = methods.IndexOf("CheckIfCanSave");
        var myMethod = methods.Skip(index).ToArray();
        return myMethod[6];
    }

    public string GetId() => _desiger.EditingActionItem.Id;
}
