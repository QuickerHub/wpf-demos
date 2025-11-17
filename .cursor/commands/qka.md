开发一个 wpf net472 的程序，参考其他目录的结构
项目结构：
name/
├── src/
│   └── projectname/
│       └── projectname.csproj
├── projectname.sln
└── README.md
├── build.yaml
├── build.ps1
├── version.json
参考 rules，使用 mvvm 开发规范
主项目的 csproj 模版

```xml
<Project Sdk="Microsoft.NET.Sdk">
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

	<PropertyGroup Condition="'$(Configuration)' == 'Release'">
		<OutputType>Library</OutputType>
		<AssemblyName>$(MSBuildProjectName).$(Version)</AssemblyName>
		<ApplicationDefinition />
	</PropertyGroup>

	<ItemGroup Condition="'$(Configuration)' == 'Release'">
		<Compile Remove="App.xaml.cs" />
		<Page Remove="App.xaml" />
		<ApplicationDefinition Remove="App.xaml" />
	</ItemGroup>

	<ItemGroup>
		<PackageReference Include="log4net" Version="2.0.15" />
		<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
		<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
	</ItemGroup>


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
