# 应用图标说明

## 图标文件

- `app-icon.svg` - 主图标设计（SVG格式）

## 设计理念

图标设计体现了"Windows 工具集"的核心概念：

1. **桌面区域**：底部蓝色区域代表 Windows 桌面
2. **桌面图标**：网格排列的白色方块代表桌面图标
3. **工具图标**：顶部金色齿轮图标代表工具/设置功能
4. **眼睛图标**：叠加在工具图标上的眼睛，表示显示/隐藏功能
5. **颜色方案**：
   - 深蓝色背景：专业和可靠
   - 蓝色桌面：Windows 系统色
   - 金色工具图标：突出工具功能
   - 白色图标：清晰可见

## 转换为 ICO 文件

要将 SVG 转换为 ICO 文件，可以使用以下方法：

### 方法1：在线转换工具
1. 访问 https://convertio.co/svg-ico/ 或 https://cloudconvert.com/svg-to-ico
2. 上传 `app-icon.svg` 文件
3. 下载生成的 ICO 文件
4. 将文件重命名为 `app-icon.ico` 并放在此目录

### 方法2：使用 ImageMagick
```bash
magick convert app-icon.svg -resize 256x256 app-icon.ico
```

### 方法3：使用 Inkscape
1. 打开 Inkscape
2. 打开 `app-icon.svg`
3. 文件 -> 导出为 -> 选择 PNG
4. 使用在线工具将 PNG 转换为 ICO

## 在项目中使用

在 `WindowsTools.csproj` 中添加：
```xml
<PropertyGroup>
  <ApplicationIcon>Assets\app-icon.ico</ApplicationIcon>
</PropertyGroup>
```

