using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
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

        public bool CanTest => IsConnected && !IsConnecting;

        public bool HasCodeEditorHandler => !string.IsNullOrEmpty(HandlerId) && HandlerId != "standalone";

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
                TestResult = $"表达式: {result.Code}\n变量数量: {result.VariableList?.Count ?? 0}";
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
                    VarType = VariableType.String,
                    DefaultValue = TestVariableValue
                };

                await _connector.ServiceClient.SetVariableForWrapperAsync(HandlerId, variable);
                TestResult = "变量设置成功";
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
                var request = new ExpressionRequest
                {
                    Code = TestExpression,
                    VariableList = new List<VariableClass>()
                };

                var result = await _connector.ServiceClient.TestExpressionForWrapperAsync(HandlerId, request);
                TestResult = FormatResult(result);
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
}

