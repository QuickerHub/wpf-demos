开发一个 wpf net472 的程序，参考其他目录的结构
项目结构：
name/
├── src/
│   └── {ProjectName}/  // 大驼峰命名
│       ├── {ProjectName}.csproj 
├── {ProjectName}.slnx
└── README.md  // 项目说明
├── build.yaml
├── build.ps1
├── version.json
参考 rules，使用 mvvm 开发规范

**项目配置文件：**

解决方案文件 {ProjectName}.slnx 模版：
```xml
<Solution>
  <Project Path="src/{ProjectName}/{ProjectName}.csproj" />
</Solution>
```

主项目的 csproj 模版：

```xml
<Project Sdk="Microsoft.NET.Sdk">
	<!-- Default project properties -->
	<PropertyGroup>
		<OutputType>WinExe</OutputType>
		<TargetFramework>net472</TargetFramework>
		<Nullable>enable</Nullable>
		<UseWPF>true</UseWPF>
		<LangVersion>preview</LangVersion>
		<PlatformTarget>x64</PlatformTarget>
		<Version Condition="'$(Version)' == ''">1.0.0.0</Version>
		<AssemblyName Condition="'$(Configuration)' != 'Release'">$(MSBuildProjectName)</AssemblyName>
	</PropertyGroup>

	<!-- Release configuration: Output as Library for Quicker integration -->
	<PropertyGroup Condition="'$(Configuration)' == 'Release'">
		<OutputType>Library</OutputType>
		<AssemblyName>$(MSBuildProjectName).$(Version)</AssemblyName>
		<ApplicationDefinition />
	</PropertyGroup>

	<!-- Remove App.xaml files in Release mode (not needed for library) -->
	<ItemGroup Condition="'$(Configuration)' == 'Release'">
		<Compile Remove="App.xaml.cs" />
		<Page Remove="App.xaml" />
		<ApplicationDefinition Remove="App.xaml" />
	</ItemGroup>

	<!-- Standard NuGet packages -->
	<ItemGroup>
		<PackageReference Include="log4net" Version="2.0.15" />
		<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
		<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
		<PackageReference Include="DependencyPropertyGenerator" Version="1.5.0">
			<PrivateAssets>all</PrivateAssets>
			<IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
		</PackageReference>
		<PackageReference Include="Costura.Fody" Version="5.7.0" />
	</ItemGroup>

	<!-- Standard framework references -->
	<ItemGroup>
		<Reference Include="Microsoft.CSharp" />
		<Reference Include="System.Net.Http" />
	</ItemGroup>

	<!-- Quicker framework references -->
	<ItemGroup>
		<Reference Include="Quicker">
			<HintPath>C:\Program Files\Quicker\Quicker.exe</HintPath>
		</Reference>
		<Reference Include="Quicker.Common">
			<HintPath>C:\Program Files\Quicker\Quicker.Common.dll</HintPath>
		</Reference>
		<Reference Include="Quicker.Public">
			<HintPath>C:\Program Files\Quicker\Quicker.Public.dll</HintPath>
		</Reference>
		<Reference Include="DotNetProjects.SVGImage">
			<HintPath>C:\Program Files\Quicker\DotNetProjects.SVGImage.dll</HintPath>
		</Reference>
		<Reference Include="DotNetProjects.Wpf.Extended.Toolkit">
			<HintPath>C:\Program Files\Quicker\DotNetProjects.Wpf.Extended.Toolkit.dll</HintPath>
		</Reference>
		<Reference Include="FontAwesomeIconsWpf">
			<HintPath>C:\Program Files\Quicker\FontAwesomeIconsWpf.dll</HintPath>
		</Reference>
		<Reference Include="HandyControl">
			<HintPath>C:\Program Files\Quicker\HandyControl.dll</HintPath>
		</Reference>
		<Reference Include="ICSharpCode.AvalonEdit">
			<HintPath>C:\Program Files\Quicker\ICSharpCode.AvalonEdit.dll</HintPath>
		</Reference>
		<Reference Include="ICSharpCode.SharpZipLib">
			<HintPath>C:\Program Files\Quicker\ICSharpCode.SharpZipLib.dll</HintPath>
		</Reference>
		<Reference Include="MdXaml">
			<HintPath>C:\Program Files\Quicker\MdXaml.dll</HintPath>
		</Reference>
	</ItemGroup>
</Project>
```
主项目的 app.xaml 模版
```xml
<Application x:Class="{projectname}.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
         <ResourceDictionary>
             <ResourceDictionary.MergedDictionaries>
                 <ResourceDictionary Source="pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml"/>
                 <ResourceDictionary Source="pack://application:,,,/HandyControl;component/Themes/Theme.xaml"/>
             </ResourceDictionary.MergedDictionaries>
         </ResourceDictionary>
    </Application.Resources>
</Application>
```

**重要说明：**
- `Properties/AssemblyInfo.cs` 应该使用 WPF 项目默认的，只包含 `ThemeInfo` 属性，不要添加其他 Assembly 信息（如 AssemblyTitle、AssemblyVersion 等）。版本信息由 .csproj 文件中的属性控制。

AssemblyInfo.cs 模版：
```csharp
using System.Windows;

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //主题特定资源词典所处位置
                                     //(未在页面中找到资源时使用，
                                     //或应用程序资源字典中找到时使用)
    ResourceDictionaryLocation.SourceAssembly //常规资源词典所处位置
                                              //(未在页面中找到资源时使用，
                                              //、应用程序或任何主题专用资源字典中找到时使用)
)]
```


**HandyControl 暗黑模式适配：**
- 在设计窗口或控件时，必须使用 HandyControl 的颜色资源来设置前景和背景，以确保自动适配暗黑模式
- 不要使用硬编码的颜色值（如 `Color="White"`、`Color="Black"`），应使用 `{DynamicResource ...}` 引用 HandyControl 的颜色资源
- **最佳实践：通常只需要在顶级控件（如 Window、UserControl）设置 Foreground，子控件会自动继承父控件的 Foreground**
- 常用的 HandyControl 颜色资源：

```xml
<!-- 前景颜色（用于 Foreground，文本颜色） -->
Foreground="{DynamicResource PrimaryTextBrush}"              <!-- 主要文本颜色（亮色/暗色自动适配） -->
Foreground="{DynamicResource SecondaryTextBrush}"            <!-- 次要文本颜色 -->
Foreground="{DynamicResource ThirdlyTextBrush}"              <!-- 第三级文本颜色 -->
<!-- ⚠️ 注意：不要使用 PrimaryBrush 作为 Foreground，它是蓝色强调色，不是文本颜色 -->

<!-- 背景颜色（用于 Background） -->
Background="{DynamicResource RegionBrush}"                    <!-- 主要区域背景 -->
Background="{DynamicResource SecondaryRegionBrush}"          <!-- 次要区域背景 -->
Background="{DynamicResource ThirdlyRegionBrush}"            <!-- 第三级区域背景 -->
Background="{DynamicResource DefaultWindowBackgroundBrush}"  <!-- 默认窗口背景 -->

<!-- 边框颜色（用于 BorderBrush） -->
BorderBrush="{DynamicResource BorderBrush}"                  <!-- 边框颜色 -->

<!-- 强调颜色（用于 Background，如按钮、高亮区域等） -->
Background="{DynamicResource PrimaryBrush}"                  <!-- 主要强调色（蓝色） -->
Background="{DynamicResource InfoBrush}"                     <!-- 信息色 -->
Background="{DynamicResource SuccessBrush}"                  <!-- 成功色 -->
Background="{DynamicResource WarningBrush}"                  <!-- 警告色 -->
Background="{DynamicResource DangerBrush}"                   <!-- 危险色 -->
```

示例（推荐方式 - 在顶级控件设置 Foreground）：
```xml
<!-- Window 示例 -->
<Window Background="{DynamicResource DefaultWindowBackgroundBrush}"
        Foreground="{DynamicResource PrimaryTextBrush}">
    <Grid Background="{DynamicResource RegionBrush}">
        <Border BorderBrush="{DynamicResource BorderBrush}"
                Background="{DynamicResource SecondaryRegionBrush}">
            <!-- TextBlock 会自动继承 Window 的 Foreground，无需重复设置 -->
            <TextBlock Text="示例文本" />
        </Border>
    </Grid>
</Window>

<!-- UserControl 示例 -->
<UserControl Background="{DynamicResource RegionBrush}"
             Foreground="{DynamicResource PrimaryTextBrush}">
    <StackPanel>
        <!-- 所有子控件会自动继承 Foreground -->
        <TextBlock Text="标题" />
        <TextBox Text="输入内容" />
        <Button Content="按钮" />
    </StackPanel>
</UserControl>
```

**重要提示：**
- 使用 `{DynamicResource ...}` 而不是 `{StaticResource ...}`，以确保主题切换时颜色能正确更新
- HandyControl 会根据系统主题自动切换亮色/暗色模式，使用颜色资源可以自动适配
- **Foreground 属性会向下继承**：在 Window/UserControl 等顶级控件设置后，子控件（如 TextBlock、Label、Button 等）会自动继承，无需在每个子控件上重复设置
- 只有在需要特殊前景色的子控件上才需要单独设置 Foreground（如强调文本、禁用状态等）
- **⚠️ 重要：Foreground 必须使用 `PrimaryTextBrush`、`SecondaryTextBrush` 等文本颜色资源，不要使用 `PrimaryBrush`（蓝色强调色）作为 Foreground**