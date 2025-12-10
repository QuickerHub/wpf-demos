# 字符串模板语法设计文档

## 1. 语法概述

模板语法采用 `{...}` 作为占位符，支持变量、格式化、方法调用、切片等多种功能。

## 2. 基础语法

### 2.1 简单变量
```
{name}        - 原文件名（不含扩展名）
{ext}         - 文件扩展名（不含点号）
{fullname}    - 完整文件名（包含扩展名）
```

### 2.2 序号变量
```
{i}           - 序号，从0开始：0, 1, 2, 3, ...
{i:00}        - 格式化序号，2位数字，从00开始：00, 01, 02, ...
{i:000}       - 格式化序号，3位数字：000, 001, 002, ...
{i:01}        - 格式化序号，从01开始：01, 02, 03, ...
{i:1}         - 序号，从1开始：1, 2, 3, ...
{i:零}        - 中文序号，从零开始：零, 一, 二, 三, ...
{i:一}        - 中文序号，从一开始：一, 二, 三, 四, ...
{i:壹}        - 大写中文序号，从壹开始：壹, 贰, 叁, 肆, ...
```

**序号格式说明**：
- `{i}` - 默认从0开始
- `{i:数字}` - 数字格式，如 `00`, `000`, `01` 等
- `{i:中文数字}` - 中文数字格式，如 `零`, `一`, `壹` 等
- 格式字符串的第一个字符决定起始值：
  - 数字格式：`00` 表示从00开始，`01` 表示从01开始
  - 中文格式：`零` 表示从零开始，`一` 表示从一开始

## 3. 字符串切片语法

### 3.1 切片语法（sub() 方法的语法糖）

`[a:b]` 是 `sub(start, end)` 方法的语法糖，提供更简洁的写法。

**基本切片**：
```
{name[:1]}    - 截取前1个字符，等同于 {name.sub(0,1)}
{name[1:]}    - 从索引1开始截取到末尾，等同于 {name.sub(1)}
{name[1:3]}   - 从索引1截取到索引3（不包含3），等同于 {name.sub(1,3)}
{name[:]}     - 完整字符串，等同于 {name} 或 {name.sub(0)}
```

**sub() 方法语法**：
```
{name.sub(start)}           - 从 start 开始截取到末尾
{name.sub(start,end)}       - 从 start 截取到 end（不包含 end）
{name.sub(0,end)}           - 从开始截取到 end（不包含 end）
```

**语法糖转换规则**：
- `{name[:b]}` → `{name.sub(0,b)}` - 从开始到索引 b
- `{name[a:]}` → `{name.sub(a)}` - 从索引 a 到末尾
- `{name[a:b]}` → `{name.sub(a,b)}` - 从索引 a 到 b（不包含 b）
- `{name[:]}` → `{name}` - 完整字符串

### 3.2 负数索引（可选，高级功能）
```
{name[:-1]}   - 截取到倒数第1个字符，等同于 {name.sub(0,-1)}
{name[-3:]}   - 从倒数第3个字符开始截取到末尾，等同于 {name.sub(-3)}
{name[-3:-1]} - 从倒数第3个字符截取到倒数第1个字符，等同于 {name.sub(-3,-1)}
```

## 4. 字符串方法调用语法

**注意**：字符串切片（`[a:b]` 和 `sub()` 方法）已在第3节说明，本节介绍其他字符串方法。

### 4.0 方法别名和语法说明

#### 4.0.1 方法别名
为了提升中文用户的体验，支持使用中文别名调用方法。方法别名通过特性（Attribute）标注。

**别名示例**：
```
{name.upper()}    等同于  {name.大写()}  或  {name.转大写()}
{name.lower()}    等同于  {name.小写()}  或  {name.转小写()}
{name.replace()}  等同于  {name.替换()}  或  {name.替换字符串()}
{name.trim()}     等同于  {name.去空白()} 或  {name.去除空白()}
```

**别名定义方式**：
在方法定义时使用 `[MethodAlias("别名1", "别名2", ...)]` 特性标注，**支持定义多个别名**：
```csharp
// 字符串扩展方法示例
public static class StringExtensions
{
    // 单个别名
    [MethodAlias("大写")]
    public static string Upper(this string str) { ... }

    // 多个别名
    [MethodAlias("小写", "转小写", "转成小写")]
    public static string Lower(this string str) { ... }

    // 多个别名，提供不同的表达方式
    [MethodAlias("替换", "替换字符串", "字符串替换")]
    public static string Replace(this string str, string old, string new) { ... }

    // 多个别名，包括简写和全称
    [MethodAlias("去空白", "去除空白", "trim")]
    public static string Trim(this string str) { ... }
}
```

**多个别名的优势**：
- ✅ **灵活性**：提供多种表达方式，满足不同用户习惯
- ✅ **兼容性**：可以同时支持中文别名和英文别名
- ✅ **易用性**：简写和全称都可以使用

**注意**：解析器通过依赖注入（DI）接收扩展类，然后从这些类中提取方法并构建别名映射表，无需全局扫描。

#### 4.0.2 括号省略语法（支持）
**设计原则**：
- ✅ **无参数方法可以省略括号**：`{name.upper()}` 可以写成 `{name.upper}`
- ✅ **属性优先原则**：如果同时存在 `length` 属性和 `length()` 方法，优先解析为属性
- ✅ **实际使用中不会冲突**：一般不会同时定义同名属性和方法
- ✅ **宽松语法解析**：支持省略空格，提升用户体验

**语法规则**：
```
{name.upper()}    等同于  {name.upper}      - 无参数方法可以省略括号
{name.upper}      优先解析为属性（如果存在），否则解析为方法调用
{name.replace(_,-)}  必须使用括号（有参数的方法）
```

**解析优先级**：
1. 首先检查是否为属性（如 `{name.length}`）
2. 如果不是属性，检查是否为无参数方法（如 `{name.upper}`）
3. 如果都不是，检查是否为有参数方法（如 `{name.replace(...)}`）

**宽松语法**：
- 支持省略空格：`{name.upper()}` 可以写成 `{name.upper()}` 或 `{name.upper}`
- **大小写忽略**：方法名大小写不敏感，`{name.upper()}`, `{name.Upper()}`, `{name.UPPER()}` 都等同于 `{name.upper()}`
- 链式调用：`{name.upper.replace(_,-)}` 等同于 `{name.upper().replace(_,-)}`

### 4.1 替换方法
```
{name.replace(old,new)}           - 替换字符串
{name.replace(old,new,count)}     - 替换指定次数
{name.replace(old,new,all)}       - 替换所有（等同于不指定count）
```

**示例**：
```
{name.replace(_,-)}               - 将下划线替换为短横线（别名：{name.替换(_,-)}）
{name.replace( ,)}                - 删除所有空格（别名：{name.替换( ,)}）
{name.replace(a,A,1)}             - 只替换第一个a为A（别名：{name.替换(a,A,1)}）
```

### 4.2 大小写转换（可选）
```
{name.upper()} 或 {name.upper}     - 转换为大写（别名：{name.大写()}、{name.转大写()} 等）
{name.Upper()} 或 {name.UPPER()}   - 同上，大小写不敏感
{name.lower()} 或 {name.lower}     - 转换为小写（别名：{name.小写()}、{name.转小写()} 等）
{name.title()} 或 {name.title}     - 首字母大写（别名：{name.首字母大写()} 或 {name.首字母大写}）
{name.capitalize()} 或 {name.capitalize} - 首字母大写，其余小写（别名：{name.首字母大写其余小写()} 或 {name.首字母大写其余小写}）
```

### 4.3 去除空白（可选）
```
{name.trim()} 或 {name.trim}      - 去除首尾空白（别名：{name.去空白()}、{name.去除空白()}、{name.trim} 等）
{name.trimStart()} 或 {name.trimStart} - 去除开头空白（别名：{name.去开头空白()}、{name.去除开头空白()} 等）
{name.trimEnd()} 或 {name.trimEnd} - 去除结尾空白（别名：{name.去结尾空白()}、{name.去除结尾空白()} 等）
```

### 4.4 填充（可选）
```
{name.padLeft(10)}                - 左侧填充空格到10位
{name.padLeft(10,0)}              - 左侧填充0到10位
{name.padRight(10)}               - 右侧填充空格到10位
{name.padRight(10,-)}             - 右侧填充-到10位
```

## 5. 组合语法

### 5.1 链式调用
```
{name.replace(_,-).upper()}        - 先替换，再转大写
{name.replace(_,-).upper}         - 同上，省略括号
{name[:5].replace( ,)}            - 先截取，再替换
{name[:5].replace( ,).upper}      - 先截取，再替换，再转大写（省略括号）
```

### 5.2 嵌套变量
```
{name.replace({ext},)}             - 在替换中使用变量（高级功能，可选）
```

## 6. 语法解析优先级

1. **变量名解析** - 首先识别变量名（如 `name`, `ext`, `i`）
2. **格式说明符** - 解析 `:` 后的格式字符串
3. **切片语法糖** - 将 `[a:b]` 转换为 `sub(a,b)` 方法调用
4. **属性/方法解析** - 解析 `.property` 或 `.method()` 部分
   - **属性优先**：如果存在同名属性，优先解析为属性
   - **无参数方法**：可以省略括号（如 `{name.upper}`）
   - **有参数方法**：必须使用括号（如 `{name.replace(...)}`）
5. **执行顺序** - 从左到右，先切片（sub），再其他方法调用

## 7. 语法示例

### 示例1：基础重命名
```
模板：{name}_{i:00}.{ext}
结果：file_00.txt, file_01.txt, file_02.txt
```

### 示例2：格式化序号
```
模板：{name}_{i:001}.{ext}
结果：file_001.txt, file_002.txt, file_003.txt
```

### 示例3：中文序号
```
模板：第{i:一}个文件.{ext}
结果：第一个文件.txt, 第二个文件.txt, 第三个文件.txt
```

### 示例4：字符串截取（使用语法糖）
```
模板：{name[:3]}_{i}.{ext}
结果：fil_0.txt, fil_1.txt, fil_2.txt
等同于：{name.sub(0,3)}_{i}.{ext}
```

### 示例4b：字符串截取（使用 sub 方法）
```
模板：{name.sub(1,5)}_{i}.{ext}
结果：ile_0.txt（如果原文件名是 file.txt）
```

### 示例5：字符串替换
```
模板：{name.replace(_,-)}.{ext}
结果：file-name.txt（如果原文件名是 file_name.txt）

模板：{name.替换(_,-)}.{ext}  （使用中文别名）
结果：file-name.txt
```

### 示例6：组合使用
```
模板：{name.replace( ,-).upper()}_{i:00}.{ext}
结果：MY-FILE_00.TXT

模板：{name.replace( ,-).Upper()}_{i:00}.{ext}  （大小写不敏感）
结果：MY-FILE_00.TXT

模板：{name.replace( ,-).upper}_{i:00}.{ext}  （省略括号）
结果：MY-FILE_00.TXT

模板：{name.替换( ,-).大写()}_{i:00}.{ext}  （使用中文别名）
结果：MY-FILE_00.TXT

模板：{name.替换( ,-).转大写()}_{i:00}.{ext}  （使用多个别名之一）
结果：MY-FILE_00.TXT

模板：{name.替换( ,-).大写}_{i:00}.{ext}  （中文别名+省略括号）
结果：MY-FILE_00.TXT
```

### 示例7：切片和方法组合
```
模板：{name[:5].replace(_,-)}.{ext}
结果：file-.txt（先截取前5个字符，再替换下划线）
等同于：{name.sub(0,5).replace(_,-)}.{ext}
```

## 8. 实现建议

### 8.1 解析器设计
1. **词法分析（Lexer）**：将模板字符串分解为 Token
   - 普通文本
   - `{` 开始标记
   - `}` 结束标记
   - 变量名
   - 操作符（`:`, `.`, `[`, `]`, `,`）

2. **语法分析（Parser）**：构建抽象语法树（AST）
   - VariableNode - 变量节点
   - FormatNode - 格式化节点
   - PropertyNode - 属性访问节点（如 `{name.length}`）
   - MethodNode - 方法调用节点（包括 sub, replace 等）
   - ChainNode - 链式调用节点
   - **注意**：
     - `[a:b]` 语法糖在解析阶段转换为 `sub(a,b)` 方法调用
     - 方法别名在解析阶段需要映射到实际方法名
     - 属性优先于方法：如果存在同名属性和方法，优先解析为属性
     - 无参数方法可以省略括号

3. **执行引擎（Evaluator）**：遍历 AST 并执行
   - 变量解析
   - 格式应用
   - 属性访问
   - 方法调用
   - 切片操作

### 8.2 实现步骤
1. **第一阶段**：实现基础变量和序号格式化
   - `{name}`, `{ext}`, `{fullname}`
   - `{i}`, `{i:00}`, `{i:01}`, `{i:1}`
   - `{i:零}`, `{i:一}`

2. **第二阶段**：实现字符串切片（sub 方法）
   - `{name.sub(start)}`, `{name.sub(start,end)}`
   - `{name[:1]}`, `{name[1:]}`, `{name[1:3]}`（语法糖形式）

3. **第三阶段**：实现方法调用和方法别名
   - `{name.replace(old,new)}` 及其中文别名 `{name.替换(old,new)}`
   - 其他字符串方法及其别名
   - 方法别名特性（Attribute）定义
   - **配置 DI 容器**：注册扩展类类型列表和模板解析器
   - **通过 DI 注入扩展类构建别名映射表**（在解析器构造函数中）
   - 别名解析和映射逻辑

4. **第四阶段**：实现链式调用和高级功能
   - 链式调用
   - 嵌套变量（可选）

### 8.3 中文数字转换
需要实现中文数字转换函数：
- 数字转中文：`0 -> 零`, `1 -> 一`, `10 -> 十`, `11 -> 十一`
- 支持小写：`零, 一, 二, ..., 十, 十一, ...`
- 支持大写：`零, 壹, 贰, ..., 拾, 拾壹, ...`
- 起始值处理：`{i:一}` 表示从1开始，`{i:零}` 表示从0开始

## 9. 错误处理

### 9.1 语法错误
- 未闭合的 `{` 或 `}`
- 无效的变量名
- 无效的方法名或别名（方法名大小写不敏感）
- 无效的参数格式
- 有参数方法调用缺少括号（无参数方法可以省略括号）

### 9.2 运行时错误
- 变量不存在
- 方法调用失败
- 索引越界
- 格式字符串无效

### 9.3 错误提示
- 在模板输入框中显示错误位置
- 提供友好的错误消息
- 预览时显示错误标记

## 10. 性能考虑

1. **模板缓存**：解析后的 AST 可以缓存
2. **增量更新**：只重新解析变化的部分
3. **批量处理**：批量处理文件列表时优化

## 11. 扩展性

设计时考虑未来可能的扩展：
- 自定义函数
- 条件表达式
- 正则表达式支持
- 日期时间格式化
- 属性访问（如 `{name.length}`，属性优先于方法）

## 12. 方法别名实现细节

### 12.1 别名特性定义
```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class MethodAliasAttribute : Attribute
{
    public string[] Aliases { get; }
    
    // 支持定义多个别名
    public MethodAliasAttribute(params string[] aliases)
    {
        if (aliases == null || aliases.Length == 0)
            throw new ArgumentException("至少需要提供一个别名", nameof(aliases));
        
        Aliases = aliases;
    }
}
```

**使用示例**：
```csharp
// 单个别名
[MethodAlias("大写")]
public static string Upper(this string str) { ... }

// 多个别名
[MethodAlias("小写", "转小写", "转成小写")]
public static string Lower(this string str) { ... }

// 多个别名，包括简写和全称
[MethodAlias("去空白", "去除空白", "trim")]
public static string Trim(this string str) { ... }
```

### 12.2 别名映射表（通过 DI 注入扩展类构建）
解析器通过依赖注入接收扩展类列表，然后从这些类中提取方法并构建别名映射表，避免全局扫描。

**实现方式**：
```csharp
public class TemplateParser
{
    private readonly Dictionary<string, MethodInfo> MethodMap = new();
    private readonly Dictionary<string, string> MethodAliasMap = new();
    
    // 通过构造函数注入扩展类类型列表
    public TemplateParser(IEnumerable<Type> extensionTypes)
    {
        BuildAliasMap(extensionTypes);
    }
    
    private void BuildAliasMap(IEnumerable<Type> extensionTypes)
    {
        foreach (var extensionType in extensionTypes)
        {
            // 扫描所有公共静态方法
            var methods = extensionType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            
            foreach (var method in methods)
            {
                // 检查是否有 MethodAlias 特性
                var aliasAttr = method.GetCustomAttribute<MethodAliasAttribute>();
                if (aliasAttr != null)
                {
                    var methodName = method.Name.ToLower(); // 统一转换为小写存储
                    
                    // 存储方法信息（用于后续调用）
                    if (!MethodMap.ContainsKey(methodName))
                    {
                        MethodMap[methodName] = method;
                    }
                    
                    // 为每个别名建立映射（别名也统一转换为小写）
                    // 支持多个别名，每个别名都映射到同一个方法名
                    foreach (var alias in aliasAttr.Aliases)
                    {
                        var aliasLower = alias.ToLower();
                        if (!MethodAliasMap.ContainsKey(aliasLower))
                        {
                            MethodAliasMap[aliasLower] = methodName;
                        }
                        // 如果别名冲突，可以记录警告或使用第一个定义
                    }
                    
                    // 同时将方法名本身也加入映射表（支持大小写不敏感）
                    if (!MethodAliasMap.ContainsKey(methodName))
                    {
                        MethodAliasMap[methodName] = methodName;
                    }
                }
            }
        }
    }
}
```

**DI 配置示例**（参考 `quicker-expression-agent.quicker` 项目）：
```csharp
// 在 App.xaml.cs 或启动类中配置 DI
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public partial class App : Application
{
    private IHost? _host;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // 注册扩展类类型列表
                services.AddSingleton<IEnumerable<Type>>(sp => new[]
                {
                    typeof(StringExtensions),
                    // 可以添加更多扩展类
                    // typeof(NumberExtensions),
                    // typeof(DateExtensions),
                });
                
                // 注册模板解析器
                services.AddSingleton<TemplateParser>(sp =>
                {
                    var extensionTypes = sp.GetRequiredService<IEnumerable<Type>>();
                    return new TemplateParser(extensionTypes);
                });
                
                // 注册 ViewModel
                services.AddTransient<BatchRenameViewModel>();
            })
            .Build();
        
        _host.Start();
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        _host?.StopAsync().GetAwaiter().GetResult();
        _host?.Dispose();
        base.OnExit(e);
    }
    
    // 提供静态方法获取服务（可选）
    public static T GetService<T>() where T : class
    {
        return ((App)Current)._host!.Services.GetRequiredService<T>();
    }
}
```

**优势**：
- ✅ **自动维护**：新增方法时只需添加 `[MethodAlias]` 特性，无需修改映射表
- ✅ **类型安全**：编译时检查，避免拼写错误
- ✅ **易于扩展**：添加新方法时自动包含别名
- ✅ **集中管理**：所有别名定义在方法上，便于查看和维护
- ✅ **依赖注入**：通过 DI 注入扩展类，避免全局扫描，提高可控性和可测试性
- ✅ **灵活配置**：可以动态选择哪些扩展类被注册，便于模块化设计

### 12.3 解析流程
1. **初始化阶段**（构造函数，通过 DI 注入）：
   - 接收扩展类类型列表（通过 DI 注入）
   - 通过反射扫描这些类中带有 `[MethodAlias]` 特性的方法
   - 自动构建别名到方法名的映射表
   - 存储方法信息（`MethodInfo`）用于后续调用
   
2. **解析阶段**：
   - 识别属性/方法名或别名（如 `upper`, `Upper`, `UPPER` 或 `大写`）
   - **大小写标准化**：将方法名统一转换为小写进行查询
   - **属性优先检查**：如果存在同名属性，优先解析为属性节点
   - 查询别名映射表（使用小写键），如果是别名则转换为实际方法名
   - 从 `MethodMap` 中获取对应的 `MethodInfo`
   - 检查是否有括号：
     - 有括号：解析为方法调用，继续解析参数
     - 无括号：检查是否为无参数方法，如果是则解析为方法调用
   - 构建 PropertyNode 或 MethodNode 节点

### 12.4 反射性能优化
由于反射操作在构造函数中执行（通过 DI 注入），只执行一次，对运行时性能影响极小：
- ✅ **单次初始化**：只在解析器实例化时执行一次
- ✅ **缓存映射表**：映射表构建后存储在实例字段中，后续直接查询
- ✅ **缓存方法信息**：`MethodInfo` 存储在字典中，避免重复反射
- ✅ **运行时无反射**：解析过程中只进行字典查询和方法调用，无反射开销
- ✅ **可控的扩展类**：通过 DI 配置，只扫描注册的扩展类，避免全局扫描

### 12.5 DI 配置说明

**项目依赖**：
需要在 `BatchRenameTool.csproj` 中添加以下 NuGet 包：
```xml
<PackageReference Include="Microsoft.Extensions.Hosting" Version="6.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="6.0.0" />
```

**扩展类定义示例**：
```csharp
// StringExtensions.cs
public static class StringExtensions
{
    [MethodAlias("大写", "转大写")]
    public static string Upper(this string str)
    {
        return str.ToUpper();
    }
    
    [MethodAlias("小写", "转小写")]
    public static string Lower(this string str)
    {
        return str.ToLower();
    }
    
    [MethodAlias("替换", "替换字符串")]
    public static string Replace(this string str, string oldValue, string newValue)
    {
        return str.Replace(oldValue, newValue);
    }
}
```

**DI 配置位置**（在 `App.xaml.cs` 中）：
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace BatchRenameTool
{
    public partial class App : Application
    {
        private IHost? _host;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    // 注册扩展类类型列表
                    services.AddSingleton<IEnumerable<Type>>(sp => new[]
                    {
                        typeof(StringExtensions),
                        // 可以添加更多扩展类
                        // typeof(NumberExtensions),
                        // typeof(DateExtensions),
                    });
                    
                    // 注册模板解析器（单例）
                    services.AddSingleton<TemplateParser>(sp =>
                    {
                        var extensionTypes = sp.GetRequiredService<IEnumerable<Type>>();
                        return new TemplateParser(extensionTypes);
                    });
                    
                    // 注册 ViewModel（瞬态）
                    services.AddTransient<BatchRenameViewModel>();
                })
                .Build();
            
            _host.Start();
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            _host?.StopAsync().GetAwaiter().GetResult();
            _host?.Dispose();
            base.OnExit(e);
        }
        
        // 提供静态方法获取服务（可选，便于在代码中获取服务）
        public static T GetService<T>() where T : class
        {
            return ((App)Current)._host!.Services.GetRequiredService<T>();
        }
    }
}
```

**在 ViewModel 中使用**：
```csharp
public partial class BatchRenameViewModel : ObservableObject
{
    private readonly TemplateParser _parser;
    
    public BatchRenameViewModel(TemplateParser parser)
    {
        _parser = parser;
    }
    
    // 或者通过 App.GetService<T>() 获取
    // public BatchRenameViewModel()
    // {
    //     _parser = App.GetService<TemplateParser>();
    // }
}
```

### 12.6 别名建议
- **支持多个别名**：可以为同一个方法定义多个别名，提供不同的表达方式
- **使用简洁、直观的中文名称**：如 `"大写"`, `"转大写"`, `"转成大写"` 等
- **避免与变量名冲突**：确保别名不与变量名（如 `name`, `ext`, `i`）冲突
- **保持命名一致性**：如都用动词形式（`"去空白"`, `"去除空白"`）
- **提供多种表达方式**：可以同时提供简写和全称（如 `"trim"` 和 `"去空白"`）
- **在方法定义时使用 `[MethodAlias]` 特性**：让系统自动发现和注册所有别名

## 13. 宽松语法解析

### 13.1 空格省略
支持在多种位置省略空格，提升用户体验：
```
{name.upper()}    等同于  {name.upper()}
{name.upper}      等同于  {name.upper}
{name.replace(_,-)} 等同于  {name.replace(_,-)}
```

### 13.2 解析规则
- **属性优先**：`{name.length}` 优先解析为属性，即使存在 `length()` 方法
- **无参数方法省略括号**：`{name.upper}` 等同于 `{name.upper()}`
- **有参数方法必须括号**：`{name.replace(_,-)}` 不能省略括号
- **方法名大小写忽略**：`{name.upper()}`, `{name.Upper()}`, `{name.UPPER()}` 都识别为同一个方法
- **链式调用**：`{name.upper.replace(_,-)}` 等同于 `{name.upper().replace(_,-)}`

### 13.3 冲突处理
如果同时存在同名属性和方法：
- **属性优先**：`{name.length}` 解析为属性访问
- **显式方法调用**：`{name.length()}` 解析为方法调用
- **实际使用中**：一般不会同时定义同名属性和方法，冲突情况极少
