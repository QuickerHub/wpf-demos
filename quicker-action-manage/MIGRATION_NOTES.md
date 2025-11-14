# 迁移说明

## 已完成
- ✅ 项目结构创建
- ✅ 项目文件 (.csproj)
- ✅ ActionManageWindow (xaml + cs)
- ✅ ActionManageControl (xaml + cs)
- ✅ SubprogramControl (xaml + cs)
- ✅ Runner.cs (提供 ActionManageWindow 方法)

## 需要迁移的文件

### ViewModel 类
需要从 `CeaQuickerTools/ViewModel` 迁移以下文件：

#### Action 相关
- `ViewModel/Action/ActionListViewModel.cs`
- `ViewModel/Action/ActionItemModel.cs`
- `ViewModel/Action/ActionItemFilter.cs`
- `ViewModel/Action/ActionItemSorter.cs`
- `ViewModel/Action/ActionRuleModel.cs`
- `ViewModel/Action/ActionRunerModel.cs`
- `ViewModel/Action/ActionSortType.cs`
- `ViewModel/Action/ActionType1.cs`

#### Subprogram 相关
- `ViewModel/Subprogram/GlobalSubprogramListModel.cs`
- `ViewModel/Subprogram/SubprogramModel.cs`
- `ViewModel/Subprogram/SubprogramFilter.cs`
- `ViewModel/Subprogram/SubprogramSorter.cs`
- `ViewModel/Subprogram/SubprogramSortType.cs`

#### 基础类
- `ViewModel/ListModel.cs` (可能被 ActionListViewModel 和 GlobalSubprogramListModel 继承)
- `ViewModel/Sorter.cs` (可能被 ActionItemSorter 和 SubprogramSorter 继承)

### View 控件和工具类
需要从 `CeaQuickerTools/View` 迁移以下文件：

- `View/PopupButtonControl.cs` (以及对应的 xaml 模板，如果有)
- `View/Menus/MenuFactory.cs`
- `View/Editor/PropertyGridPlus.cs` (以及相关编辑器类)
- `View/Converters/DateTimeShortConverter.cs` (如果有)

### 依赖的工具类
这些可能需要从 `CeaQuickerTools` 或其他项目迁移：

- `Cea.Utils` 命名空间下的工具类 (UIHelper, CommonUtil 等)
- `Cea.Utils.Extension` 命名空间下的扩展方法
- `Quicker.Utilities` 命名空间下的工具类 (QuickerUtil, UIHelper 等)

### 资源文件
- Theme.xaml (如果使用了主题资源)
- 其他静态资源定义

## 注意事项

1. **命名空间更新**: 所有迁移的文件需要将命名空间从 `CeaQuickerTools.*` 更新为 `QuickerActionManage.*`

2. **依赖处理**: 
   - 某些依赖可能来自外部库 (如 `Cea.Utils`, `Cea.Utils.Extension`)
   - 需要确认这些依赖是否可以直接引用，或者需要一起迁移

3. **资源引用**: 
   - XAML 中的静态资源引用需要确保资源文件也被迁移
   - 图标资源 (如 `GlobalSubProgramIcon`) 需要确保可用

4. **Runner 类**: 
   - `Runner.cs` 中的 `ShowWindow` 方法需要实现完整的窗口显示逻辑
   - 可能需要参考原项目中的 `ViewRunner.ShowWindow` 方法

5. **测试**: 
   - 迁移完成后需要测试所有功能是否正常
   - 确保所有依赖都正确引用

## 下一步

1. 迁移 ViewModel 类
2. 迁移 View 控件和工具类
3. 处理依赖关系
4. 更新 Runner 类中的 ShowWindow 实现
5. 测试功能完整性

