# API Key 安全存储方案调研

## 概述

本文档调研了在已发布的桌面应用程序（WPF）中安全存储 API Key 的各种方案，分析了各自的优缺点和适用场景。

## 核心原则

**重要提醒**：对于桌面应用程序，**完全防止 API Key 泄露在技术上是不可能的**。任何存储在客户端的数据都可以被提取。我们的目标是：
1. **增加提取难度**：让普通用户难以提取
2. **延迟泄露时间**：增加专业逆向工程师的提取成本
3. **降低泄露影响**：使用 API Key 限制、监控和轮换机制

## 方案对比

### 方案 1：编译时嵌入 + 代码混淆/加密（当前方案）

**实现方式**：
- 编译时将 API Key 嵌入到代码中
- 使用代码混淆工具（如 ConfuserEx、Obfuscar）
- 使用 exe 加密工具（如 VMProtect、Themida）

**优点**：
- ✅ 实现简单
- ✅ 不需要额外的运行时依赖
- ✅ 与代码混淆结合可以提供一定保护

**缺点**：
- ❌ 仍然可以通过反编译提取（只是难度增加）
- ❌ 专业逆向工程师仍然可以提取
- ❌ 代码混淆可能影响性能
- ❌ 加密工具可能被杀毒软件误报

**适用场景**：
- 对安全性要求不是特别高的场景
- 主要防止普通用户提取
- 配合 API Key 使用限制和监控

**推荐指数**：⭐⭐⭐（中等）

---

### 方案 2：Windows DPAPI（Data Protection API）

**实现方式**：
- 使用 Windows DPAPI 加密存储 API Key
- 可以基于用户或机器级别加密
- 存储在注册表或配置文件中

**优点**：
- ✅ Windows 原生支持，无需额外依赖
- ✅ 加密密钥由 Windows 管理，相对安全
- ✅ 基于用户或机器的加密，其他用户无法解密
- ✅ 实现相对简单

**缺点**：
- ❌ 只能在 Windows 系统上使用
- ❌ 如果用户重装系统或更换用户，数据会丢失
- ❌ 仍然可以通过调试器在运行时提取
- ❌ 需要处理首次运行时的初始化

**实现示例**：
```csharp
using System.Security.Cryptography;
using System.Text;

public class SecureStorage
{
    // Encrypt API key using DPAPI
    public static string Encrypt(string plainText)
    {
        byte[] data = Encoding.UTF8.GetBytes(plainText);
        byte[] encrypted = ProtectedData.Protect(
            data, 
            null, // Optional entropy
            DataProtectionScope.CurrentUser // or LocalMachine
        );
        return Convert.ToBase64String(encrypted);
    }

    // Decrypt API key using DPAPI
    public static string Decrypt(string encryptedText)
    {
        byte[] data = Convert.FromBase64String(encryptedText);
        byte[] decrypted = ProtectedData.Unprotect(
            data,
            null,
            DataProtectionScope.CurrentUser
        );
        return Encoding.UTF8.GetString(decrypted);
    }
}
```

**适用场景**：
- Windows 桌面应用
- 需要跨会话持久化存储
- 对安全性有一定要求

**推荐指数**：⭐⭐⭐⭐（推荐）

---

### 方案 3：AES 加密 + 密钥派生

**实现方式**：
- 使用 AES 加密 API Key
- 密钥通过 PBKDF2 从用户输入或机器特征派生
- 存储加密后的数据

**优点**：
- ✅ 跨平台支持
- ✅ 可以基于用户输入（密码）派生密钥
- ✅ 相对安全，即使文件被复制也无法直接解密

**缺点**：
- ❌ 如果基于机器特征，重装系统会丢失
- ❌ 如果基于用户输入，需要用户记住密码
- ❌ 仍然可以通过调试器在运行时提取
- ❌ 实现复杂度较高

**实现示例**：
```csharp
using System.Security.Cryptography;
using System.Text;

public class AesEncryption
{
    private const int KeySize = 256;
    private const int Iterations = 100000;

    public static string Encrypt(string plainText, string password)
    {
        byte[] salt = GenerateSalt();
        byte[] key = DeriveKey(password, salt);
        
        using (var aes = Aes.Create())
        {
            aes.KeySize = KeySize;
            aes.Key = key;
            aes.GenerateIV();
            
            using (var encryptor = aes.CreateEncryptor())
            using (var ms = new MemoryStream())
            {
                ms.Write(salt, 0, salt.Length);
                ms.Write(aes.IV, 0, aes.IV.Length);
                
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations))
        {
            return pbkdf2.GetBytes(KeySize / 8);
        }
    }
}
```

**适用场景**：
- 需要跨平台支持
- 可以要求用户输入密码
- 对安全性要求较高

**推荐指数**：⭐⭐⭐（中等）

---

### 方案 4：服务器代理模式

**实现方式**：
- API Key 存储在服务器端
- 客户端通过自己的服务器代理访问第三方 API
- 客户端使用自己的认证机制

**优点**：
- ✅ **最安全的方案**：API Key 完全不暴露在客户端
- ✅ 可以实施访问控制和限流
- ✅ 可以监控和审计 API 使用
- ✅ 可以轻松轮换 API Key

**缺点**：
- ❌ 需要维护服务器
- ❌ 增加延迟和成本
- ❌ 需要处理服务器认证
- ❌ 架构复杂度高

**适用场景**：
- 对安全性要求极高
- 有服务器资源
- 需要集中管理和监控

**推荐指数**：⭐⭐⭐⭐⭐（最推荐，如果条件允许）

---

### 方案 5：混合方案：首次运行时配置 + DPAPI 加密

**实现方式**：
- 首次运行时要求用户输入或配置 API Key
- 使用 DPAPI 加密后存储
- 后续运行时自动解密使用

**优点**：
- ✅ 结合了用户配置和加密存储
- ✅ API Key 不编译到程序中
- ✅ 每个用户使用自己的 API Key
- ✅ 相对安全

**缺点**：
- ❌ 需要用户配置，体验稍差
- ❌ 仍然可以通过调试器提取
- ❌ 需要处理配置丢失的情况

**适用场景**：
- 希望用户使用自己的 API Key
- 不想在程序中硬编码
- 可以接受首次配置

**推荐指数**：⭐⭐⭐⭐（推荐）

---

### 方案 6：环境变量 + 配置文件（开发/测试用）

**实现方式**：
- 从环境变量或配置文件读取
- 不编译到程序中

**优点**：
- ✅ 简单直接
- ✅ 适合开发和测试

**缺点**：
- ❌ 安全性最低
- ❌ 配置文件可能被泄露
- ❌ 不适合生产环境

**适用场景**：
- 仅用于开发和测试
- 不推荐用于生产环境

**推荐指数**：⭐（仅开发用）

---

## 综合推荐方案

### 方案 A：DPAPI 加密存储（推荐用于单用户场景）

**实现步骤**：
1. 首次运行时，从嵌入式配置或用户输入获取 API Key
2. 使用 DPAPI 加密后存储到配置文件或注册表
3. 后续运行时自动解密使用
4. 配合代码混淆增加提取难度

**优点**：
- 平衡了安全性和易用性
- Windows 原生支持
- 实现相对简单

### 方案 B：服务器代理（推荐用于高安全场景）

**实现步骤**：
1. API Key 存储在服务器
2. 客户端通过自己的 API 访问
3. 服务器代理转发到第三方 API
4. 实施访问控制和监控

**优点**：
- 最安全
- 可以集中管理

### 方案 C：混合方案（推荐用于多用户场景）

**实现步骤**：
1. 首次运行时要求用户配置自己的 API Key
2. 使用 DPAPI 加密存储
3. 提供配置界面供用户更新
4. 配合代码混淆

**优点**：
- 用户使用自己的 API Key
- 不硬编码在程序中
- 相对安全

---

## 额外安全措施

无论选择哪种方案，都应该配合以下措施：

### 1. API Key 使用限制
- 在 API 提供商处设置使用限制（IP、频率、配额）
- 设置使用监控和告警

### 2. 代码混淆
- 使用 ConfuserEx、Obfuscar 等工具
- 混淆关键代码路径

### 3. 反调试保护
- 检测调试器附加
- 检测虚拟机环境
- 使用反调试库

### 4. 运行时检查
- 检测程序是否被修改
- 检测运行环境异常

### 5. 定期轮换
- 定期更换 API Key
- 提供更新机制

### 6. 监控和日志
- 监控 API Key 使用情况
- 记录异常访问
- 及时响应安全事件

---

## 实施建议

### 对于当前项目（QuickerExpressionAgent）

**推荐方案**：**方案 5（混合方案）** 或 **方案 2（DPAPI）**

**实施步骤**：
1. 保留当前的嵌入式配置作为默认值（用于首次运行）
2. 首次运行时，使用 DPAPI 加密 API Key 并存储
3. 后续运行时优先使用加密存储的值
4. 提供配置界面允许用户更新 API Key
5. 配合代码混淆工具

**代码结构**：
```
ConfigurationService
├── 读取优先级：
│   1. DPAPI 加密存储的配置（用户配置）
│   2. 嵌入式配置（默认值，首次运行）
│   3. appsettings.json（开发用）
│   4. 环境变量（开发用）
└── 提供配置界面更新 API Key
```

---

## 总结

| 方案 | 安全性 | 易用性 | 实现复杂度 | 推荐场景 |
|------|--------|--------|------------|----------|
| 编译嵌入+混淆 | ⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐ | 低安全要求 |
| DPAPI 加密 | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | 单用户桌面应用 |
| AES 加密 | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ | 跨平台应用 |
| 服务器代理 | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | 高安全要求 |
| 混合方案 | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | 多用户场景 |
| 环境变量 | ⭐ | ⭐⭐⭐⭐⭐ | ⭐ | 仅开发用 |

**最终建议**：
- **短期**：使用 DPAPI 加密存储，配合代码混淆
- **长期**：考虑服务器代理模式，提供最佳安全性
- **通用**：无论哪种方案，都要配合 API Key 限制、监控和轮换机制

---

## 参考资料

- [Windows Data Protection API (DPAPI)](https://learn.microsoft.com/en-us/previous-versions/ms995355(v=msdn.10))
- [OWASP API Security](https://owasp.org/www-project-api-security/)
- [.NET Cryptography](https://learn.microsoft.com/en-us/dotnet/standard/security/cryptography-model)
- [ConfuserEx - .NET Code Protection](https://github.com/mkaring/ConfuserEx)

