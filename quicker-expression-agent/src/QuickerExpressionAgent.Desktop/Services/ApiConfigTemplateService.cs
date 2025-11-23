using QuickerExpressionAgent.Server.Services;
using System.Collections.Generic;

namespace QuickerExpressionAgent.Desktop.Services;

/// <summary>
/// Service for providing API configuration templates for different LLM providers
/// </summary>
public class ApiConfigTemplateService
{
    /// <summary>
    /// Get all available API configuration templates
    /// </summary>
    public static List<ApiConfigTemplate> GetTemplates()
    {
        return new List<ApiConfigTemplate>
        {
            // 自定义配置（最前面）
            new ApiConfigTemplate
            {
                Name = "自定义配置",
                Description = "手动输入 API 配置",
                Config = new ModelApiConfig
                {
                    ApiKey = string.Empty,
                    ModelId = string.Empty,
                    BaseUrl = string.Empty,
                    Title = "custom"
                }
            },
            // 中国内的API
            new ApiConfigTemplate
            {
                Name = "DeepSeek",
                Description = "DeepSeek Chat API (支持 Function Calling)",
                Config = new ModelApiConfig
                {
                    ApiKey = string.Empty,
                    ModelId = "deepseek-chat",
                    BaseUrl = "https://api.deepseek.com/v1",
                    Title = "deepseek"
                }
            },
            new ApiConfigTemplate
            {
                Name = "Moonshot AI",
                Description = "Moonshot AI API (支持 Function Calling)",
                Config = new ModelApiConfig
                {
                    ApiKey = string.Empty,
                    ModelId = "moonshot-v1-8k",
                    BaseUrl = "https://api.moonshot.cn/v1",
                    Title = "moonshot"
                }
            },
            new ApiConfigTemplate
            {
                Name = "智谱 AI (GLM-4.6)",
                Description = "智谱 AI GLM-4.6 API (支持 Function Calling)",
                Config = new ModelApiConfig
                {
                    ApiKey = string.Empty,
                    ModelId = "glm-4.6",
                    BaseUrl = "https://open.bigmodel.cn/api/paas/v4",
                    Title = "glm-4.6"
                }
            },
            // 国际API
            new ApiConfigTemplate
            {
                Name = "OpenAI GPT-4 Turbo",
                Description = "OpenAI GPT-4 Turbo API (支持 Function Calling)",
                Config = new ModelApiConfig
                {
                    ApiKey = string.Empty,
                    ModelId = "gpt-4-turbo-preview",
                    BaseUrl = "https://api.openai.com/v1",
                    Title = "gpt-4-turbo"
                }
            },
            new ApiConfigTemplate
            {
                Name = "Anthropic Claude Sonnet 4.5",
                Description = "Anthropic Claude Sonnet 4.5 API (支持 Function Calling, 最新版本)",
                Config = new ModelApiConfig
                {
                    ApiKey = string.Empty,
                    ModelId = "claude-sonnet-4-5-20250929",
                    BaseUrl = "https://api.anthropic.com/v1",
                    Title = "claude-4.5"
                }
            },
            new ApiConfigTemplate
            {
                Name = "Anthropic Claude Haiku 4.5",
                Description = "Anthropic Claude Haiku 4.5 API (支持 Function Calling, 快速响应)",
                Config = new ModelApiConfig
                {
                    ApiKey = string.Empty,
                    ModelId = "claude-haiku-4-5-20251001",
                    BaseUrl = "https://api.anthropic.com/v1",
                    Title = "claude-haiku-4.5"
                }
            },
            new ApiConfigTemplate
            {
                Name = "Anthropic Claude Opus 4.1",
                Description = "Anthropic Claude Opus 4.1 API (支持 Function Calling, 最强性能)",
                Config = new ModelApiConfig
                {
                    ApiKey = string.Empty,
                    ModelId = "claude-opus-4-1-20250805",
                    BaseUrl = "https://api.anthropic.com/v1",
                    Title = "claude-opus-4.1"
                }
            },
            new ApiConfigTemplate
            {
                Name = "Google Gemini 2.0 Flash",
                Description = "Google Gemini 2.0 Flash API (支持 Function Calling, 最新版本)",
                Config = new ModelApiConfig
                {
                    ApiKey = string.Empty,
                    ModelId = "gemini-2.0-flash-exp",
                    BaseUrl = "https://generativelanguage.googleapis.com/v1",
                    Title = "gemini-2.0"
                }
            }
        };
    }
}

/// <summary>
/// API configuration template
/// </summary>
public class ApiConfigTemplate
{
    /// <summary>
    /// Template name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Configuration template
    /// </summary>
    public ModelApiConfig Config { get; set; } = new();
}

