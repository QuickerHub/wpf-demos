# 贴边隐藏动作说明

## 功能描述
将窗口隐藏到屏幕边缘，当鼠标离开窗口时自动隐藏，鼠标移动到边缘时自动显示。

## 参数说明

### 基本参数

1. **窗口句柄（windowHandle）**
   - 类型：整数（int）
   - 说明：要进行贴边隐藏操作的目标窗口的句柄
   - 必填：是

2. **边缘方向（edgeDirection）**
   - 类型：字符串（string）
   - 可选值：
     - `"Left"` - 左侧边缘
     - `"Top"` - 顶部边缘
     - `"Right"` - 右侧边缘
     - `"Bottom"` - 底部边缘
     - `"Nearest"` - 自动选择最近的边缘（默认值）
   - 默认值：`"Nearest"`

3. **可见区域（visibleArea）**
   - 类型：字符串（string）
   - 格式：
     - `"5"` - 所有边都是 5 像素
     - `"5,6"` - 水平 5 像素，垂直 6 像素
     - `"1,2,3,4"` - 左 1，上 2，右 3，下 4 像素
   - 说明：窗口隐藏后仍然可见的区域大小（可以为负数，表示扩大检测区域）
   - 默认值：`"5"`

4. **动画类型（animationType）**
   - 类型：字符串（string）
   - 可选值：
     - `"None"` - 无动画，直接移动（默认值）
     - `"Linear"` - 线性动画
   - 默认值：`"None"`

5. **屏幕边缘显示（showOnScreenEdge）**
   - 类型：布尔值（bool）
   - 说明：当鼠标移动到屏幕边缘时是否显示窗口
   - 默认值：`false`

6. **自动取消注册（autoUnregister）**
   - 类型：布尔值（bool）
   - 说明：如果窗口已启用贴边隐藏，再次调用时是否自动取消
   - 默认值：`true`

7. **自动置顶（autoTopmost）**
   - 类型：布尔值（bool）
   - 说明：是否自动将窗口设置为置顶
   - 默认值：`false`

### 命令参数（quicker_param）

通过 `quicker_param` 参数可以执行特殊命令或覆盖边缘方向：

#### 管理命令

1. **manage**
   - 说明：打开管理窗口，查看所有已注册贴边隐藏的窗口
   - 示例：`quicker_param = "manage"`
   - 注意：使用此命令时，`windowHandle` 参数会被忽略

2. **stopall**
   - 说明：取消所有窗口的贴边隐藏
   - 示例：`quicker_param = "stopall"`
   - 注意：使用此命令时，`windowHandle` 参数会被忽略

#### 边缘方向覆盖

以下命令会覆盖 `edgeDirection` 参数：

- `"left"` - 强制使用左侧边缘
- `"top"` - 强制使用顶部边缘
- `"right"` - 强制使用右侧边缘
- `"bottom"` - 强制使用底部边缘
- `"auto"` - 自动选择最近的边缘（等同于 `"Nearest"`）

**注意**：当使用 `quicker_param` 时，`autoUnregister` 会被强制设置为 `false`。

## 使用示例

### 示例 1：基本使用
```
windowHandle: 123456
edgeDirection: "Left"
visibleArea: "5"
animationType: "None"
```

### 示例 2：使用线性动画
```
windowHandle: 123456
edgeDirection: "Nearest"
visibleArea: "-5"
animationType: "Linear"
autoTopmost: true
```

### 示例 3：打开管理窗口
```
windowHandle: 0 (任意值，会被忽略)
quicker_param: "manage"
```

### 示例 4：取消所有贴边隐藏
```
windowHandle: 0 (任意值，会被忽略)
quicker_param: "stopall"
```

### 示例 5：使用命令参数指定边缘方向
```
windowHandle: 123456
quicker_param: "right"
visibleArea: "5"
animationType: "Linear"
```

## 返回值

返回一个包含以下属性的对象：
- `Success`（bool）：操作是否成功
- `Message`（string）：操作结果消息

## 注意事项

1. 不支持对系统窗口（桌面、任务栏等）启用贴边隐藏
2. 窗口最小化时，贴边隐藏功能会自动失效
3. 使用 `quicker_param` 命令时，`autoUnregister` 会被强制设置为 `false`
4. 配置会自动保存到本地 JSON 文件，应用重启后会自动恢复

