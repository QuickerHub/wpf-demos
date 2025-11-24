using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickerExpressionAgent.Common;
using System;
using System.Collections.Generic;
using System.Linq;
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

    [ObservableProperty]
    private VariableType _selectedVariableType = VariableType.String;

    [ObservableProperty]
    private List<VariableClass> _testVariables = new();

    [ObservableProperty]
    private string _testVariablesJson = string.Empty;

    public bool CanTest => !IsConnecting;

    public bool HasCodeEditorHandler => !string.IsNullOrEmpty(HandlerId) && HandlerId != "standalone";

    public VariableType[] VariableTypes
    {
        get
        {
#if NET8_0_OR_GREATER
            return Enum.GetValues<VariableType>();
#elif NET472_OR_GREATER
            return [.. Enum.GetValues(typeof(VariableType)).Cast<VariableType>()];
#else
            return Enum.GetValues(typeof(VariableType)).Cast<VariableType>().ToArray();
#endif
        }
    }

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
            
            object variablesList;
            if (result.VariableList != null && result.VariableList.Count > 0)
            {
                variablesList = result.VariableList.Select(v => new
                {
                    v.VarName,
                    v.VarType,
                    DefaultValue = v.DefaultValue,
                    DeserializedValue = v.GetDefaultValue()
                }).ToList();
            }
            else
            {
                variablesList = new List<object>();
            }
            
            var resultObj = new
            {
                Expression = result.Code ?? string.Empty,
                VariableCount = result.VariableList?.Count ?? 0,
                Variables = variablesList
            };
            
            TestResult = resultObj.ToJson(indented: true);
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
                VarType = SelectedVariableType
            };
            variable.SetDefaultValue(ParseVariableValue(TestVariableValue, SelectedVariableType));

            await _service.SetVariableForWrapperAsync(HandlerId, variable);
            TestResult = $"变量设置成功: {variable.VarName} ({variable.VarType}) = {variable.DefaultValue}";
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
    private async Task TestGetAllVariablesAsync()
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
            var variables = result.VariableList ?? new List<VariableClass>();
            
            var variablesObj = variables.Select(v => new
            {
                v.VarName,
                v.VarType,
                DefaultValue = v.DefaultValue,
                DeserializedValue = v.GetDefaultValue()
            }).ToList();
            
            TestResult = variablesObj.ToJson(indented: true);
            TestVariables = variables;
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
            List<VariableClass>? variables = null;
            
            // Parse test variables from JSON if provided
            if (!string.IsNullOrWhiteSpace(TestVariablesJson))
            {
                try
                {
                    variables = TestVariablesJson.FromJson<List<VariableClass>>();
                }
                catch (Exception ex)
                {
                    TestResult = $"解析测试变量 JSON 失败: {ex.Message}";
                    return;
                }
            }
            
            var request = new ExpressionRequest
            {
                Code = TestExpression,
                VariableList = variables ?? []
            };

            var result = await _service.TestExpressionForWrapperAsync(HandlerId, request);
            TestResult = result.ToJson(indented: true);
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
    private void LoadTestVariablesFromJson()
    {
        if (string.IsNullOrWhiteSpace(TestVariablesJson))
        {
            TestResult = "请输入测试变量的 JSON";
            return;
        }

        try
        {
            TestVariables = TestVariablesJson.FromJson<List<VariableClass>>() ?? new List<VariableClass>();
            TestResult = $"成功加载 {TestVariables.Count} 个测试变量";
        }
        catch (Exception ex)
        {
            TestResult = $"解析 JSON 失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportVariablesToJson()
    {
        if (TestVariables.Count == 0)
        {
            TestResult = "没有可导出的变量";
            return;
        }

        try
        {
            TestVariablesJson = TestVariables.ToJson(indented: true);
            TestResult = $"成功导出 {TestVariables.Count} 个变量到 JSON";
        }
        catch (Exception ex)
        {
            TestResult = $"导出失败: {ex.Message}";
        }
    }

    private object? ParseVariableValue(string value, VariableType varType)
    {
        // Use existing ConvertValueFromStringSafe extension method
        return varType.ConvertValueFromStringSafe(value);
    }

}

