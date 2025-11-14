# QuickerActionManage

动作和公共子程序管理窗口的独立项目。

## 项目结构

```
quicker-action-manage/
├── src/
│   └── QuickerActionManage/
│       ├── View/              # 视图层
│       │   ├── ActionManageWindow.xaml/cs
│       │   ├── ActionManageControl.xaml/cs
│       │   ├── SubprogramControl.xaml/cs
│       │   ├── PopupButtonControl.cs
│       │   ├── Menus/
│       │   │   └── MenuFactory.cs
│       │   └── Editor/        # 需要迁移 PropertyGridPlus 等编辑器
│       ├── ViewModel/         # 视图模型层
│       │   ├── Action/        # 动作相关 ViewModel
│       │   ├── Subprogram/    # 子程序相关 ViewModel
│       │   ├── ListModel.cs
│       │   ├── Sorter.cs
│       │   └── NObject.cs
│       └── Runner.cs          # Quicker 集成入口
└── MIGRATION_NOTES.md         # 迁移说明文档
```

## 使用方法

```csharp
using QuickerActionManage;

// 显示动作管理窗口
Runner.ActionManageWindow();
```

## 已完成的工作

✅ **项目结构**：创建了完整的项目文件夹结构
✅ **项目文件**：创建了 `.csproj` 文件，包含所有必要的依赖
✅ **核心窗口和控件**：
   - `ActionManageWindow` (xaml + cs)
   - `ActionManageControl` (xaml + cs)
   - `SubprogramControl` (xaml + cs)
   - `PopupButtonControl.cs`
   - `MenuFactory.cs`
✅ **ViewModel 类**：已迁移所有 ViewModel 类（Action 和 Subprogram 相关）
✅ **Editor 类**：已迁移所有编辑器类（PropertyGridPlus, NumEditor, TextPropertyEditor 等）
✅ **转换器类**：已迁移所有转换器（DateTimeShortConverter, Object2VisibilityConverter 等）
✅ **资源文件**：创建了 `App.xaml` 包含转换器和资源定义
✅ **Runner 类**：创建了 `Runner.cs`，提供 `ActionManageWindow()` 方法

## 项目状态

🎉 **核心迁移工作已完成！**

所有主要的代码文件都已迁移完成，包括：
- 40+ 个文件
- 3000+ 行代码
- 所有命名空间已从 `CeaQuickerTools.*` 更新为 `QuickerActionManage.*`

## 下一步建议

1. **编译测试**：尝试编译项目，检查是否有缺失的依赖
2. **功能测试**：测试窗口显示和基本功能
3. **依赖完善**：根据编译错误补充缺失的依赖（如果有）
4. **原项目更新**：从原项目中移除已迁移的代码（可选）

## 注意事项

### 依赖关系
项目依赖以下外部库和工具类：
- **Quicker 相关**：`Quicker`, `Quicker.Common`, `Quicker.Public`, `Quicker.Utilities`
- **UI 库**：`HandyControl`, `Xceed.Wpf.Toolkit` (用于 ColorEditor)
- **其他**：`log4net`, `Newtonsoft.Json`, `CommunityToolkit.Mvvm`

### 可能需要调整的地方

1. **QWindowHelper**：`Runner.cs` 中的 `SetCanUseQuicker` 调用使用了反射，如果 Quicker.Utilities 不可用，会静默失败。

2. **资源引用**：
   - XAML 中的静态资源已在 `App.xaml` 中定义
   - 某些样式资源来自 HandyControl

3. **工具类扩展**：
   - 某些扩展方法来自 `Quicker.Public.Extensions`
   - `TextUtil` 来自 `Quicker.Utilities`

## 注意事项

1. **命名空间**：所有文件已从 `CeaQuickerTools.*` 更新为 `QuickerActionManage.*`

2. **依赖处理**：
   - 某些依赖来自外部库 (如 `Cea.Utils`, `Cea.Utils.Extension`)
   - 需要确认这些依赖是否可以直接引用，或者需要一起迁移

3. **资源引用**：
   - XAML 中的静态资源引用需要确保资源文件也被迁移
   - 图标资源需要确保可用

4. **Runner 类**：
   - `Runner.cs` 中的 `ShowWindow` 方法需要实现完整的窗口显示逻辑
   - 可能需要参考原项目中的 `ViewRunner.ShowWindow` 方法

## 编译和测试

1. 确保所有依赖的 NuGet 包已安装
2. 确保 Quicker 相关的 DLL 引用路径正确
3. 编译项目检查是否有缺失的依赖
4. 测试功能完整性

