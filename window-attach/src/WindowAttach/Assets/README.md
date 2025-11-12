# 应用图标说明

## 图标文件

- `app-icon.svg` - 主图标设计（SVG格式）
- `app-icon-simple.svg` - 简化版图标设计

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

## 图标设计说明

图标设计理念：
- 两个窗口（一大一小）表示窗口吸附的概念
- 金色连接线表示吸附关系
- 蓝色背景表示专业和可靠
- 简洁的设计确保在小尺寸下也能清晰识别

## 在项目中使用

在 `WindowAttach.csproj` 中添加：
```xml
<PropertyGroup>
  <ApplicationIcon>Assets\app-icon.ico</ApplicationIcon>
</PropertyGroup>
```

