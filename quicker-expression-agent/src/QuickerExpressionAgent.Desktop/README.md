# Quicker Expression Agent Desktop

WPF 桌面应用程序，用于体验 AI 表达式生成功能。

## 功能特性

- 🎨 现代化的 WPF 界面
- 🤖 AI 驱动的表达式生成
- ✅ 自动测试和验证生成的表达式
- 🔄 自动迭代优化（最多 3 次）
- 📊 实时显示生成状态和结果

## 使用方法

1. **运行应用程序**
   ```bash
   dotnet run --project src/QuickerExpressionAgent.Desktop
   ```

2. **输入自然语言描述**
   - 在"自然语言描述"文本框中输入您想要生成的表达式描述
   - 例如：
     - "计算 1 + 2"
     - "生成一个1到100的随机数"
     - "计算两个字符串的相似度"

3. **点击"生成表达式"按钮**
   - 应用程序会自动调用 AI 生成 C# 表达式
   - 自动执行并测试生成的表达式
   - 如果失败，会自动迭代优化（最多 3 次）

4. **查看结果**
   - "生成的表达式"区域显示 AI 生成的 C# 代码
   - "执行结果"区域显示执行结果或错误信息
   - 状态栏显示当前状态和进度

## 界面说明

- **输入区域**：输入自然语言描述
- **生成结果区域**：
  - 生成的表达式：显示 AI 生成的 C# 代码
  - 执行结果：显示执行结果（成功/失败）
- **状态栏**：显示当前状态和进度指示器

## 配置

配置文件：`appsettings.json`

```json
{
  "OpenAI": {
    "ApiKey": "your-api-key",
    "ModelId": "deepseek-chat",
    "BaseUrl": "https://api.deepseek.com/v1"
  }
}
```

## 技术栈

- .NET 8.0 WPF
- CommunityToolkit.Mvvm（MVVM 模式）
- Microsoft Semantic Kernel（AI 框架）
- Roslyn（表达式执行）

