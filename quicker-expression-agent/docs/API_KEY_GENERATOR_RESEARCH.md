# API Key 代码生成器方案调研

## 概述

本文档调研了用于在编译时生成 API Key 存储代码的各种方案，包括 MSBuild Task、Source Generator、T4 模板等。

## 核心问题

**目标**：在编译时生成代码（`.g.cs` 文件），将 API Key 以某种形式嵌入到程序中，同时增加提取难度。

**挑战**：
- 完全防止提取在技术上不可能
- 目标是增加提取难度和成本
- 需要平衡安全性和易用性

---

## 方案对比

### 方案 1：MSBuild Task（当前方案）

**实现方式**：
- 创建自定义 MSBuild Task
- 在编译前读取 `.env` 文件
- 生成 `.g.cs` 文件包含配置值

**优点**：
- ✅ 实现简单，易于理解
- ✅ 完全控制生成逻辑
- ✅ 可以轻松扩展功能
- ✅ 支持复杂的转换逻辑

**缺点**：
- ❌ 需要单独的项目来编译 Task
- ❌ 生成的是明文字符串（当前实现）
- ❌ 容易被反编译提取

**当前实现**：
```csharp
// 生成简单的字符串属性
public static string ApiKey => "sk-xxx";
```

**适用场景**：
- 基础需求
- 需要快速实现
- 配合代码混淆使用

**推荐指数**：⭐⭐⭐

---

### 方案 2：增强版 MSBuild Task（推荐）

**实现方式**：
- 基于当前 MSBuild Task
- 生成时对字符串进行加密/混淆
- 生成解密代码

**优点**：
- ✅ 基于现有方案，易于迁移
- ✅ 可以生成混淆的代码
- ✅ 增加提取难度
- ✅ 完全控制加密算法

**缺点**：
- ❌ 仍然可以通过调试器提取
- ❌ 需要实现加密/解密逻辑

**生成示例**：
```csharp
// 生成加密的字符串和解密方法
private static readonly byte[] _encryptedApiKey = new byte[] { 0x12, 0x34, ... };
public static string ApiKey => Decrypt(_encryptedApiKey);
```

**适用场景**：
- 需要增强安全性
- 基于现有方案改进
- 不想引入新依赖

**推荐指数**：⭐⭐⭐⭐

---

### 方案 3：.NET Source Generator

**实现方式**：
- 使用 .NET 5+ 的 Source Generator API
- 在编译时分析代码或文件
- 生成代码片段

**优点**：
- ✅ .NET 官方支持
- ✅ 集成到编译流程
- ✅ 可以分析现有代码
- ✅ 支持增量生成

**缺点**：
- ❌ 需要 .NET 5+ 或更高版本
- ❌ 学习曲线较陡
- ❌ 调试相对困难
- ❌ 仍然生成到程序集中

**实现示例**：
```csharp
[Generator]
public class ApiKeyGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        // 读取 .env 文件
        // 生成加密代码
        context.AddSource("ApiKeyConfig.g.cs", source);
    }
}
```

**适用场景**：
- 使用 .NET 5+ 或更高版本
- 需要深度集成到编译流程
- 需要分析现有代码

**推荐指数**：⭐⭐⭐⭐

---

### 方案 4：T4 模板（Text Template Transformation Toolkit）

**实现方式**：
- 使用 Visual Studio 的 T4 模板
- 在编译时或设计时生成代码
- 支持复杂的模板逻辑

**优点**：
- ✅ Visual Studio 原生支持
- ✅ 模板语法简单
- ✅ 可以在设计时预览
- ✅ 支持复杂逻辑

**缺点**：
- ❌ 主要依赖 Visual Studio
- ❌ 跨平台支持有限
- ❌ 需要手动触发或配置
- ❌ 调试相对困难

**实现示例**：
```t4
<#@ template language="C#" #>
<#@ output extension=".g.cs" #>
namespace MyApp.Generated {
    public static class ApiKeyConfig {
        public static string ApiKey => "<#= Encrypt(ReadEnv("API_KEY")) #>";
    }
}
```

**适用场景**：
- Visual Studio 开发环境
- 需要设计时预览
- 简单的代码生成需求

**推荐指数**：⭐⭐⭐

---

### 方案 5：Roslyn Analyzer + Code Fix

**实现方式**：
- 创建 Roslyn Analyzer
- 分析代码中的特殊标记
- 生成代码修复建议或自动生成

**优点**：
- ✅ 深度集成到 IDE
- ✅ 可以提供代码提示
- ✅ 支持自动修复

**缺点**：
- ❌ 实现复杂度高
- ❌ 主要用于代码分析
- ❌ 不适合简单的代码生成

**适用场景**：
- 需要 IDE 集成
- 复杂的代码分析需求

**推荐指数**：⭐⭐

---

## 增强方案：加密/混淆生成器

### 方案 A：XOR 加密生成器

**原理**：
- 使用 XOR 加密字符串
- 生成加密字节数组
- 生成简单的解密方法

**生成代码示例**：
```csharp
internal static class EmbeddedConfig
{
    private static readonly byte[] _encryptedApiKey = new byte[] 
    { 
        0x45, 0x23, 0x67, 0x89, ... // XOR 加密后的字节
    };
    
    private static readonly byte[] _key = new byte[] { 0x12, 0x34, 0x56, 0x78 };
    
    public static string ApiKey
    {
        get
        {
            var decrypted = new byte[_encryptedApiKey.Length];
            for (int i = 0; i < _encryptedApiKey.Length; i++)
            {
                decrypted[i] = (byte)(_encryptedApiKey[i] ^ _key[i % _key.Length]);
            }
            return Encoding.UTF8.GetString(decrypted);
        }
    }
}
```

**优点**：
- ✅ 实现简单
- ✅ 运行时解密快速
- ✅ 增加提取难度

**缺点**：
- ❌ XOR 加密较弱
- ❌ 仍然可以通过调试器提取

---

### 方案 B：字符串分割 + 拼接生成器

**原理**：
- 将字符串分割成多个片段
- 打乱顺序存储
- 运行时拼接

**生成代码示例**：
```csharp
internal static class EmbeddedConfig
{
    private static readonly string[] _parts = new[]
    {
        "sk-",
        "8d4b",
        "453e",
        "b4aa",
        "4bc9",
        // ...
    };
    
    private static readonly int[] _order = new[] { 0, 2, 1, 4, 3, ... };
    
    public static string ApiKey
    {
        get
        {
            var sb = new StringBuilder();
            foreach (var idx in _order)
            {
                sb.Append(_parts[idx]);
            }
            return sb.ToString();
        }
    }
}
```

**优点**：
- ✅ 增加静态分析难度
- ✅ 实现简单
- ✅ 性能影响小

**缺点**：
- ❌ 仍然可以通过调试器提取
- ❌ 字符串片段可能暴露模式

---

### 方案 C：Base64 + 简单变换生成器

**原理**：
- 将字符串转换为 Base64
- 进行简单的字符替换或移位
- 运行时还原

**生成代码示例**：
```csharp
internal static class EmbeddedConfig
{
    private static readonly string _encoded = "c2stOGQ0YjQ1M2ViNGFhNGJjOWI5Nz...";
    
    public static string ApiKey
    {
        get
        {
            // 简单的字符替换还原
            var decoded = _encoded.Replace('_', '/').Replace('-', '+');
            var bytes = Convert.FromBase64String(decoded);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
```

**优点**：
- ✅ 实现简单
- ✅ 可以结合多种变换

**缺点**：
- ❌ 安全性较低
- ❌ 容易被识别

---

### 方案 D：混合混淆生成器（推荐）

**原理**：
- 结合多种技术：
  - XOR 加密
  - 字符串分割
  - Base64 编码
  - 随机变量名
  - 虚假代码注入

**生成代码示例**：
```csharp
internal static class EmbeddedConfig
{
    // 虚假的变量，增加混淆
    private static readonly int _dummy1 = 0x1234;
    private static readonly string _dummy2 = "fake";
    
    // 真实的加密数据
    private static readonly byte[] _data1 = new byte[] { 0x12, 0x34, ... };
    private static readonly byte[] _data2 = new byte[] { 0x56, 0x78, ... };
    private static readonly int[] _indices = new[] { 1, 0, 2, ... };
    
    public static string ApiKey
    {
        get
        {
            // 复杂的解密逻辑
            var parts = new List<byte[]>();
            foreach (var idx in _indices)
            {
                var part = DecryptPart(_data1, _data2, idx);
                parts.Add(part);
            }
            return CombineParts(parts);
        }
    }
    
    // 虚假方法，增加混淆
    private static void FakeMethod() { }
}
```

**优点**：
- ✅ 安全性最高（在生成器方案中）
- ✅ 增加静态和动态分析难度
- ✅ 可以持续改进

**缺点**：
- ❌ 实现复杂度高
- ❌ 可能影响性能
- ❌ 仍然可以通过调试器提取

---

## 推荐实现方案

### 短期方案：增强版 MSBuild Task（XOR 加密）

**理由**：
- 基于现有方案，易于实现
- 增加提取难度
- 实现简单，维护成本低

**实施步骤**：
1. 修改 `GenerateEmbeddedConfigTask`
2. 添加 XOR 加密逻辑
3. 生成加密字节数组和解密代码
4. 使用随机密钥（每次编译不同）

### 中期方案：混合混淆生成器

**理由**：
- 更高的安全性
- 可以持续改进
- 平衡安全性和性能

**实施步骤**：
1. 扩展 `GenerateEmbeddedConfigTask`
2. 实现多种混淆技术
3. 生成复杂的解密代码
4. 添加虚假代码注入

### 长期方案：Source Generator

**理由**：
- .NET 官方支持
- 更好的集成
- 支持增量生成

**实施步骤**：
1. 迁移到 .NET 5+（如果可能）
2. 实现 Source Generator
3. 集成到编译流程

---

## 现有工具和库

### 1. 代码混淆工具（配合使用）

- **ConfuserEx**：开源代码混淆工具
- **Obfuscar**：开源代码混淆工具
- **VMProtect**：商业保护工具
- **Themida**：商业保护工具

**建议**：无论使用哪种生成器，都应该配合代码混淆工具使用。

### 2. 字符串加密库

- **BabelObfuscator**：商业混淆工具，支持字符串加密
- **Eazfuscator.NET**：商业混淆工具

**注意**：这些工具通常在编译后处理，而生成器是在编译时生成代码。

---

## 实施建议

### 对于当前项目

**推荐方案**：**增强版 MSBuild Task（XOR 加密 + 字符串分割）**

**理由**：
1. 基于现有方案，迁移成本低
2. 可以快速实现
3. 增加提取难度
4. 配合代码混淆效果更好

**实施优先级**：
1. ✅ 实现 XOR 加密生成器（简单，快速）
2. ✅ 添加字符串分割混淆（中等复杂度）
3. ⏳ 添加虚假代码注入（可选，高级）
4. ⏳ 考虑迁移到 Source Generator（长期）

---

## 总结

| 方案 | 实现难度 | 安全性 | 维护成本 | 推荐度 |
|------|----------|--------|----------|--------|
| 当前 MSBuild Task | ⭐ | ⭐ | ⭐ | ⭐⭐⭐ |
| 增强 MSBuild Task | ⭐⭐ | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐ |
| Source Generator | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ |
| T4 模板 | ⭐⭐ | ⭐⭐ | ⭐⭐ | ⭐⭐⭐ |
| 混合混淆生成器 | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |

**最终建议**：
- **立即实施**：增强版 MSBuild Task（XOR 加密）
- **中期改进**：添加字符串分割和虚假代码
- **长期考虑**：迁移到 Source Generator（如果升级到 .NET 5+）

**重要提醒**：
- 任何生成器方案都不能完全防止提取
- 必须配合代码混淆工具使用
- 应该实施 API Key 使用限制和监控
- 考虑服务器代理模式作为最安全的方案

