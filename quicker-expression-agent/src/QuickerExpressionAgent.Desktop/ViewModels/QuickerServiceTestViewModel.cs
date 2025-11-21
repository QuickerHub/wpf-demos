using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace QuickerExpressionAgent.Desktop.ViewModels
{
    public partial class QuickerServiceTestViewModel : ObservableObject
    {
        private readonly ILogger<QuickerServiceTestViewModel>? _logger;
        private readonly QuickerServerClientConnector _connector;

        [ObservableProperty]
        private bool _isConnected;

        [ObservableProperty]
        private string _statusText = "未连接";

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

        public bool CanTest => IsConnected && !IsConnecting;

        public bool HasCodeEditorHandler => !string.IsNullOrEmpty(HandlerId) && HandlerId != "standalone";

        public Array VariableTypes => Enum.GetValues(typeof(VariableType));

        partial void OnIsConnectedChanged(bool value)
        {
            OnPropertyChanged(nameof(CanTest));
        }

        partial void OnHandlerIdChanged(string value)
        {
            OnPropertyChanged(nameof(HasCodeEditorHandler));
        }

        public QuickerServiceTestViewModel(
            QuickerServerClientConnector connector,
            ILogger<QuickerServiceTestViewModel>? logger = null)
        {
            _connector = connector;
            _logger = logger;

            // Subscribe to connection status changes
            connector.ConnectionStatusChanged += (sender, isConnected) =>
            {
                UpdateConnectionStatus(isConnected);
            };

            // Initialize connection status
            UpdateConnectionStatus(connector.IsConnected);
        }

        private void UpdateConnectionStatus(bool isConnected)
        {
            IsConnected = isConnected;
            StatusText = isConnected ? "已连接" : "未连接";
        }

        [RelayCommand]
        private async Task ConnectAsync()
        {
            if (_connector.IsConnected)
            {
                StatusText = "已连接";
                return;
            }

            IsConnecting = true;
            StatusText = "正在连接...";
            TestResult = string.Empty;

            try
            {
                // Wait for connection (connector will auto-connect in background)
                var connected = await _connector.WaitConnectAsync(TimeSpan.FromSeconds(10));
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsConnecting = false;
                    if (!connected)
                    {
                        StatusText = "连接超时";
                        TestResult = "连接超时，请确保 Quicker 服务正在运行";
                    }
                    // Connection status will be updated via ConnectionStatusChanged event
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsConnecting = false;
                    IsConnected = false;
                    StatusText = $"连接失败: {ex.Message}";
                    TestResult = $"连接错误: {ex.Message}";
                    _logger?.LogError(ex, "Failed to connect to Quicker service");
                });
            }
        }

        [RelayCommand]
        private async Task DisconnectAsync()
        {
            try
            {
                await _connector.StopAsync(CancellationToken.None);
                // Connection status will be updated via ConnectionStatusChanged event
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disconnecting");
                // Connection status will be updated via ConnectionStatusChanged event
            }
        }


        [RelayCommand]
        private async Task TestGetOrCreateCodeEditorAsync()
        {
            if (!_connector.IsConnected)
            {
                TestResult = "未连接";
                return;
            }

            IsConnecting = true;
            TestResult = "获取中...";

            try
            {
                var handlerId = await _connector.ServiceClient.GetOrCreateCodeEditorAsync();
                HandlerId = handlerId;
                TestResult = $"获取成功，Handler ID: {handlerId}";
            }
            catch (Exception ex)
            {
                TestResult = $"错误: {ex.Message}";
                _logger?.LogError(ex, "Error getting or creating code editor");
            }
            finally
            {
                IsConnecting = false;
            }
        }

        [RelayCommand]
        private async Task TestGetCodeWrapperIdAsync()
        {
            if (!_connector.IsConnected)
            {
                TestResult = "未连接";
                return;
            }

            IsConnecting = true;
            TestResult = "获取中...";

            try
            {
                var windowHandle = "0"; // Test with zero handle (should return "standalone")
                var handlerId = await _connector.ServiceClient.GetCodeWrapperIdAsync(windowHandle);
                HandlerId = handlerId;
                TestResult = $"获取成功，Handler ID: {handlerId}";
            }
            catch (Exception ex)
            {
                TestResult = $"错误: {ex.Message}";
                _logger?.LogError(ex, "Error getting code wrapper ID");
            }
            finally
            {
                IsConnecting = false;
            }
        }

        [RelayCommand]
        private async Task TestGetExpressionAndVariablesAsync()
        {
            if (!_connector.IsConnected)
            {
                TestResult = "未连接";
                return;
            }

            if (!HasCodeEditorHandler)
            {
                TestResult = "请先调用 GetOrCreateCodeEditorAsync 获取 Code Editor Handler ID";
                return;
            }

            IsConnecting = true;
            TestResult = "获取中...";

            try
            {
                var result = await _connector.ServiceClient.GetExpressionAndVariablesForWrapperAsync(HandlerId);
                
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
                
                TestResult = resultObj.ToJson();
            }
            catch (Exception ex)
            {
                TestResult = $"错误: {ex.Message}";
                _logger?.LogError(ex, "Error getting expression and variables");
            }
            finally
            {
                IsConnecting = false;
            }
        }

        [RelayCommand]
        private async Task TestSetExpressionForWrapperAsync()
        {
            if (!_connector.IsConnected)
            {
                TestResult = "未连接";
                return;
            }

            if (!HasCodeEditorHandler)
            {
                TestResult = "请先调用 GetOrCreateCodeEditorAsync 获取 Code Editor Handler ID";
                return;
            }

            IsConnecting = true;
            TestResult = "设置中...";

            try
            {
                await _connector.ServiceClient.SetExpressionForWrapperAsync(HandlerId, TestExpression);
                TestResult = "表达式设置成功";
            }
            catch (Exception ex)
            {
                TestResult = $"错误: {ex.Message}";
                _logger?.LogError(ex, "Error setting expression for wrapper");
            }
            finally
            {
                IsConnecting = false;
            }
        }

        [RelayCommand]
        private async Task TestGetVariableAsync()
        {
            if (!_connector.IsConnected)
            {
                TestResult = "未连接";
                return;
            }

            if (!HasCodeEditorHandler)
            {
                TestResult = "请先调用 GetOrCreateCodeEditorAsync 获取 Code Editor Handler ID";
                return;
            }

            IsConnecting = true;
            TestResult = "获取中...";

            try
            {
                var variable = await _connector.ServiceClient.GetVariableForWrapperAsync(HandlerId, TestVariableName);
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
                _logger?.LogError(ex, "Error getting variable");
            }
            finally
            {
                IsConnecting = false;
            }
        }

        [RelayCommand]
        private async Task TestSetVariableAsync()
        {
            if (!_connector.IsConnected)
            {
                TestResult = "未连接";
                return;
            }

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

                await _connector.ServiceClient.SetVariableForWrapperAsync(HandlerId, variable);
                TestResult = $"变量设置成功: {variable.VarName} ({variable.VarType}) = {variable.DefaultValue}";
            }
            catch (Exception ex)
            {
                TestResult = $"错误: {ex.Message}";
                _logger?.LogError(ex, "Error setting variable");
            }
            finally
            {
                IsConnecting = false;
            }
        }

        [RelayCommand]
        private async Task TestGetAllVariablesAsync()
        {
            if (!_connector.IsConnected)
            {
                TestResult = "未连接";
                return;
            }

            if (!HasCodeEditorHandler)
            {
                TestResult = "请先调用 GetOrCreateCodeEditorAsync 获取 Code Editor Handler ID";
                return;
            }

            IsConnecting = true;
            TestResult = "获取中...";

            try
            {
                var result = await _connector.ServiceClient.GetExpressionAndVariablesForWrapperAsync(HandlerId);
                var variables = result.VariableList ?? new List<VariableClass>();
                
                var variablesObj = variables.Select(v => new
                {
                    v.VarName,
                    v.VarType,
                    DefaultValue = v.DefaultValue,
                    DeserializedValue = v.GetDefaultValue()
                }).ToList();
                
                TestResult = variablesObj.ToJson();
                TestVariables = variables;
            }
            catch (Exception ex)
            {
                TestResult = $"错误: {ex.Message}";
                _logger?.LogError(ex, "Error getting all variables");
            }
            finally
            {
                IsConnecting = false;
            }
        }

        [RelayCommand]
        private async Task TestTestExpressionForWrapperAsync()
        {
            if (!_connector.IsConnected)
            {
                TestResult = "未连接";
                return;
            }

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

                var result = await _connector.ServiceClient.TestExpressionForWrapperAsync(HandlerId, request);
                TestResult = result.ToJson();
            }
            catch (Exception ex)
            {
                TestResult = $"错误: {ex.Message}";
                _logger?.LogError(ex, "Error testing expression for wrapper");
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
                TestVariablesJson = TestVariables.ToJson();
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
}

