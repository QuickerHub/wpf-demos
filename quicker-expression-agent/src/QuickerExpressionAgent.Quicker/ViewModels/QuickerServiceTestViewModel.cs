using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickerExpressionAgent.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Windows;

namespace QuickerExpressionAgent.Quicker.ViewModels;

public partial class QuickerServiceTestViewModel : ObservableObject
{
    private readonly IQuickerService _service;

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private string _testResult = string.Empty;

    [ObservableProperty]
    private bool _isConnecting = false;

    [ObservableProperty]
    private string _handlerId = string.Empty;

    [ObservableProperty]
    private string _testExpression = "\"Hello\" + \" World\"";

    [ObservableProperty]
    private string _testVariableName = "testVar";

    [ObservableProperty]
    private string _testVariableValue = "testValue";

    public bool CanTest => !IsConnecting;

    public bool HasCodeEditorHandler => !string.IsNullOrEmpty(HandlerId) && HandlerId != "standalone";

    partial void OnHandlerIdChanged(string value)
    {
        OnPropertyChanged(nameof(HasCodeEditorHandler));
    }

    public QuickerServiceTestViewModel(IQuickerService service)
    {
        _service = service;
    }

    [RelayCommand]
    private async Task TestGetOrCreateCodeEditorAsync()
    {
        IsConnecting = true;
        TestResult = "获取中...";

        try
        {
            var handlerId = await _service.GetOrCreateCodeEditorAsync();
            HandlerId = handlerId;
            TestResult = $"获取成功，Handler ID: {handlerId}";
        }
        catch (Exception ex)
        {
            TestResult = $"错误: {ex.Message}";
        }
        finally
        {
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private async Task TestGetCodeWrapperIdAsync()
    {
        IsConnecting = true;
        TestResult = "获取中...";

        try
        {
            var windowHandle = "0"; // Test with zero handle (should return "standalone")
            var handlerId = await _service.GetCodeWrapperIdAsync(windowHandle);
            HandlerId = handlerId;
            TestResult = $"获取成功，Handler ID: {handlerId}";
        }
        catch (Exception ex)
        {
            TestResult = $"错误: {ex.Message}";
        }
        finally
        {
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private async Task TestGetExpressionAndVariablesAsync()
    {
        if (!HasCodeEditorHandler)
        {
            TestResult = "请先调用 GetOrCreateCodeEditorAsync 获取 Code Editor Handler ID";
            return;
        }

        IsConnecting = true;
        TestResult = "获取中...";

        try
        {
            var result = await _service.GetExpressionAndVariablesForWrapperAsync(HandlerId);
            TestResult = $"表达式: {result.Code}\n变量数量: {result.VariableList?.Count ?? 0}";
        }
        catch (Exception ex)
        {
            TestResult = $"错误: {ex.Message}";
        }
        finally
        {
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private async Task TestSetExpressionForWrapperAsync()
    {
        if (!HasCodeEditorHandler)
        {
            TestResult = "请先调用 GetOrCreateCodeEditorAsync 获取 Code Editor Handler ID";
            return;
        }

        IsConnecting = true;
        TestResult = "设置中...";

        try
        {
            await _service.SetExpressionForWrapperAsync(HandlerId, TestExpression);
            TestResult = "表达式设置成功";
        }
        catch (Exception ex)
        {
            TestResult = $"错误: {ex.Message}";
        }
        finally
        {
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private async Task TestGetVariableAsync()
    {
        if (!HasCodeEditorHandler)
        {
            TestResult = "请先调用 GetOrCreateCodeEditorAsync 获取 Code Editor Handler ID";
            return;
        }

        IsConnecting = true;
        TestResult = "获取中...";

        try
        {
            var variable = await _service.GetVariableForWrapperAsync(HandlerId, TestVariableName);
            if (variable != null)
            {
                TestResult = $"变量: {variable.VarName}, 类型: {variable.VarType}, 值: {variable.GetDefaultValue()}";
            }
            else
            {
                TestResult = "变量不存在";
            }
        }
        catch (Exception ex)
        {
            TestResult = $"错误: {ex.Message}";
        }
        finally
        {
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private async Task TestSetVariableAsync()
    {
        if (!HasCodeEditorHandler)
        {
            TestResult = "请先调用 GetOrCreateCodeEditorAsync 获取 Code Editor Handler ID";
            return;
        }

        IsConnecting = true;
        TestResult = "设置中...";

        try
        {
            var variable = new VariableClass
            {
                VarName = TestVariableName,
                VarType = VariableType.String,
                DefaultValue = TestVariableValue
            };

            await _service.SetVariableForWrapperAsync(HandlerId, variable);
            TestResult = "变量设置成功";
        }
        catch (Exception ex)
        {
            TestResult = $"错误: {ex.Message}";
        }
        finally
        {
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private async Task TestTestExpressionForWrapperAsync()
    {
        if (!HasCodeEditorHandler)
        {
            TestResult = "请先调用 GetOrCreateCodeEditorAsync 获取 Code Editor Handler ID";
            return;
        }

        IsConnecting = true;
        TestResult = "测试中...";

        try
        {
            var request = new ExpressionRequest
            {
                Code = TestExpression,
                VariableList = []
            };

            var result = await _service.TestExpressionForWrapperAsync(HandlerId, request);
            TestResult = FormatResult(result);
        }
        catch (Exception ex)
        {
            TestResult = $"错误: {ex.Message}";
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private string FormatResult(ExpressionResult result)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) // Support Chinese characters
        };

        var usedVars = result.UsedVariables?.Select(v => new { v.VarName, v.VarType }).ToList();
        var resultObj = new
        {
            Success = result.Success,
            Value = result.Value?.ToString() ?? "null",
            Error = result.Error,
            UsedVariables = usedVars != null ? (object)usedVars : new List<object>()
        };

        return JsonSerializer.Serialize(resultObj, jsonOptions);
    }
}

