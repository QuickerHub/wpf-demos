# 应用图标说明

## 图标文件

- `app-icon.svg` - 主图标设计（SVG格式，渐变风格）
- `app-icon-simple.svg` - 简化版图标设计（扁平风格）

## 设计理念

图标设计体现了"贴边隐藏"的核心概念：

1. **窗口在屏幕边缘**：窗口大部分隐藏在屏幕外，只有一小部分可见
2. **双向箭头**：
   - 向左的箭头表示窗口隐藏到屏幕外
   - 向右的小箭头表示窗口可以从边缘显示
3. **视觉层次**：
   - 可见部分使用实线填充
   - 隐藏部分使用虚线轮廓
   - 屏幕边缘有高亮指示

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

在 `WindowEdgeHide.csproj` 中添加：
```xml
<PropertyGroup>
  <ApplicationIcon>Assets\app-icon.ico</ApplicationIcon>
</PropertyGroup>
```

## 颜色方案

### app-icon.svg（渐变风格）
- 背景：深蓝色渐变 (#1e3a5f → #2d4a6f)
- 窗口：蓝色渐变 (#4a90e2 → #357abd)
- 箭头：金色渐变 (#ffd700 → #ffb300)
- 屏幕边缘：白色半透明

### app-icon-simple.svg（扁平风格）
- 背景：深灰色渐变 (#2c3e50 → #34495e)
- 窗口：蓝色渐变 (#3498db → #2980b9)
- 箭头：橙色 (#f39c12)
- 屏幕边缘：浅灰色 (#ecf0f1)

