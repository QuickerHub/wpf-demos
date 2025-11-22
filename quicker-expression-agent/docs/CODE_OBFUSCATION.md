# 代码混淆配置指南

## 概述

本文档介绍如何在 Release 编译时配置代码混淆，以保护 API Key 和代码逻辑。

## 方案 1：使用 Obfuscar（推荐）

Obfuscar 是一个开源的 .NET 代码混淆器，支持 .NET 8.0。

### 安装步骤

1. **添加 NuGet 包引用**

在 `QuickerExpressionAgent.Server.csproj` 中添加：

```xml
<ItemGroup>
  <PackageReference Include="Obfuscar" Version="2.2.37">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

2. **创建混淆配置文件**

创建 `obfuscar.xml` 文件：

```xml
<?xml version="1.0" encoding="utf-8"?>
<Obfuscator>
  <!-- 输入目录 -->
  <Var name="InPath" value="bin\Release\net8.0" />
  
  <!-- 输出目录 -->
  <Var name="OutPath" value="bin\Release\net8.0\obfuscated" />
  
  <!-- 要混淆的程序集 -->
  <Module file="$(InPath)\QuickerExpressionAgent.Server.exe" />
  
  <!-- 排除规则 -->
  <Assembly name="QuickerExpressionAgent.Server">
    <!-- 保留 EmbeddedConfig 类（包含 API Key） -->
    <Type name="QuickerExpressionAgent.Server.Generated.EmbeddedConfig" />
    
    <!-- 保留公共接口 -->
    <Type name="QuickerExpressionAgent.Server.Services.IConfigurationService" />
    
    <!-- 保留 Program 入口点 -->
    <Type name="QuickerExpressionAgent.Server.Program">
      <Method name="Main" />
    </Type>
  </Assembly>
  
  <!-- 混淆选项 -->
  <Var name="KeepPublicApi" value="false" />
  <Var name="HidePrivateApi" value="true" />
  <Var name="RenameProperties" value="true" />
  <Var name="RenameEvents" value="true" />
  <Var name="RenameFields" value="true" />
  <Var name="UseUnicodeNames" value="true" />
  <Var name="UseKoreanNames" value="false" />
  <Var name="ReuseNames" value="true" />
  <Var name="HideStrings" value="true" />
  <Var name="OptimizeMethods" value="true" />
  <Var name="SuppressIldasm" value="true" />
</Obfuscator>
```

3. **在项目文件中集成**

在 `QuickerExpressionAgent.Server.csproj` 中添加：

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <ObfuscarEnabled>true</ObfuscarEnabled>
</PropertyGroup>

<Target Name="Obfuscate" AfterTargets="Build" Condition="'$(Configuration)' == 'Release' AND '$(ObfuscarEnabled)' == 'true'">
  <Exec Command="dotnet $(NuGetPackageRoot)obfuscar\2.2.37\tools\Obfuscar.Console.exe obfuscar.xml" 
        WorkingDirectory="$(MSBuildProjectDirectory)" />
  
  <!-- 将混淆后的文件复制回输出目录 -->
  <ItemGroup>
    <ObfuscatedFiles Include="bin\Release\net8.0\obfuscated\**\*.*" />
  </ItemGroup>
  <Copy SourceFiles="@(ObfuscatedFiles)" 
        DestinationFiles="@(ObfuscatedFiles->'bin\Release\net8.0\%(RecursiveDir)%(Filename)%(Extension)')" 
        OverwriteReadOnlyFiles="true" />
</Target>
```

## 方案 2：使用 ConfuserEx

ConfuserEx 是另一个强大的开源混淆器。

### 安装步骤

1. **下载 ConfuserEx**
   - 从 GitHub 下载：https://github.com/mkaring/ConfuserEx/releases
   - 解压到项目目录的 `tools\ConfuserEx` 文件夹

2. **创建配置文件 `confuser.crproj`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<project baseDir="bin\Release\net8.0" outputDir="bin\Release\net8.0\obfuscated" xmlns="http://confuser.codeplex.com">
  <rule pattern="true" inherit="false">
    <protection id="anti ildasm" />
    <protection id="anti tamper" />
    <protection id="constants" />
    <protection id="ctrl flow" />
    <protection id="invalid metadata" />
    <protection id="ref proxy" />
    <protection id="rename">
      <argument name="mode" value="unicode" />
      <argument name="flatten" value="true" />
    </protection>
    <protection id="resources" />
  </rule>
  
  <module path="QuickerExpressionAgent.Server.exe">
    <rule pattern="namespace('QuickerExpressionAgent.Server.Generated')" inherit="false">
      <protection id="rename" action="remove" />
    </rule>
  </module>
</project>
```

3. **在项目文件中集成**

```xml
<Target Name="Confuse" AfterTargets="Build" Condition="'$(Configuration)' == 'Release'">
  <Exec Command="tools\ConfuserEx\Confuser.CLI.exe confuser.crproj" 
        WorkingDirectory="$(MSBuildProjectDirectory)" />
  
  <!-- 复制混淆后的文件 -->
  <ItemGroup>
    <ConfusedFiles Include="bin\Release\net8.0\obfuscated\**\*.*" />
  </ItemGroup>
  <Copy SourceFiles="@(ConfusedFiles)" 
        DestinationFiles="@(ConfusedFiles->'bin\Release\net8.0\%(RecursiveDir)%(Filename)%(Extension)')" 
        OverwriteReadOnlyFiles="true" />
</Target>
```

## 方案 3：使用 Dotfuscator Community Edition

Dotfuscator 是 Visual Studio 内置的混淆工具（社区版免费）。

### 配置步骤

1. **启用 Dotfuscator**
   - 在 Visual Studio 中，右键项目 → 属性
   - 在"生成"选项卡中，勾选"启用 Dotfuscator"

2. **配置混淆规则**
   - 点击"编辑规则"按钮
   - 选择要混淆的程序集
   - 配置混淆级别

## 方案 4：使用第三方工具（如你提到的加密工具）

如果你已经有加密工具，可以在编译后手动运行，或者通过 MSBuild 任务集成：

```xml
<Target Name="PostBuildEncrypt" AfterTargets="Build" Condition="'$(Configuration)' == 'Release'">
  <Exec Command="YourEncryptionTool.exe &quot;$(TargetPath)&quot;" 
        WorkingDirectory="$(MSBuildProjectDirectory)" />
</Target>
```

## 注意事项

### 1. 保留必要的类型

混淆时需要保留以下类型，否则可能导致运行时错误：

- `EmbeddedConfig` 类（包含 API Key）
- 公共接口（如 `IConfigurationService`）
- `Program.Main` 入口点
- 序列化相关的类型
- 反射使用的类型

### 2. 测试混淆后的程序

混淆可能会影响程序运行，建议：

- 在混淆后进行全面测试
- 确保所有功能正常工作
- 检查 API 调用是否正常

### 3. 性能影响

混淆可能会：

- 略微增加程序启动时间
- 增加程序集大小
- 影响调试体验（Release 版本通常不需要调试）

### 4. 与 Source Generator 的兼容性

Source Generator 生成的代码（如 `EmbeddedConfig.generated.cs`）也会被混淆，但：

- API Key 的值不会被混淆（字符串字面量）
- 类名和方法名会被混淆
- 建议保留 `EmbeddedConfig` 类名以便访问

## 推荐配置

对于本项目，推荐使用 **Obfuscar**，因为：

1. ✅ 开源免费
2. ✅ 支持 .NET 8.0
3. ✅ 可以通过 NuGet 包管理
4. ✅ 配置灵活
5. ✅ 可以集成到 MSBuild 流程

## 示例配置

完整的 `obfuscar.xml` 配置示例：

```xml
<?xml version="1.0" encoding="utf-8"?>
<Obfuscator>
  <Var name="InPath" value="bin\Release\net8.0" />
  <Var name="OutPath" value="bin\Release\net8.0\obfuscated" />
  
  <Module file="$(InPath)\QuickerExpressionAgent.Server.exe" />
  
  <Assembly name="QuickerExpressionAgent.Server">
    <!-- 保留 EmbeddedConfig（包含 API Key） -->
    <Type name="QuickerExpressionAgent.Server.Generated.EmbeddedConfig">
      <Field name="ApiKey" />
    </Type>
    
    <!-- 保留公共接口 -->
    <Type name="QuickerExpressionAgent.Server.Services.IConfigurationService" />
    
    <!-- 保留 Program 入口点 -->
    <Type name="QuickerExpressionAgent.Server.Program">
      <Method name="Main" />
    </Type>
  </Assembly>
  
  <!-- 混淆选项 -->
  <Var name="HideStrings" value="true" />
  <Var name="SuppressIldasm" value="true" />
  <Var name="RenameProperties" value="true" />
  <Var name="RenameEvents" value="true" />
  <Var name="RenameFields" value="true" />
</Obfuscator>
```

## 验证混淆效果

混淆后，可以使用以下工具验证：

1. **ILSpy** - 反编译查看混淆效果
2. **dnSpy** - 另一个反编译工具
3. **Reflexil** - IL 编辑器

如果混淆成功，反编译后的代码应该：

- 类名、方法名、变量名被重命名
- 字符串被隐藏或加密
- 控制流被混淆
- 难以理解和修改

