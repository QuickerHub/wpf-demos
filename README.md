# wpf-demos

Quicker 集成的 WPF / 工具示例集合。各子目录为独立项目，统一使用 [qkbuild](https://github.com/QuickerHub/quicker_build_net) 构建与发布 Quicker 依赖包。

## qkbuild 用法（各子目录）

在子项目根目录执行 `build.ps1`（内部调用 `scripts/Invoke-Qkbuild.ps1`）：

| 场景 | 命令 |
|------|------|
| 本地 Release 构建 | `.\build.ps1` |
| 热更新测试（revision +1，写入 `_packages` + OSS） | `.\build.ps1 -Test` 或 `.\build.ps1 -t` |
| 发布到 Quicker 官方（交互选版本） | `.\build.ps1 -Publish` |
| 发布当前 `version.json` 版本 | `.\build.ps1 -Publish -NoVersion` |
| 指定四段版本发布 | `.\build.ps1 -Publish --version 1.2.3.0` |

**注意**：PowerShell 中勿单独使用裸 `-p` / `-n`（会与通用参数冲突），请用 `-Publish` / `-NoVersion` 或 `--publish` / `--no-version`。

### 环境变量（`.env` 或 qkbuild 安装目录）

| 变量 | 用途 |
|------|------|
| `QUICKER_EMAIL` / `QUICKER_PASSWORD` | 首次登录 getquicker，上传依赖包 |
| `BITIFUL_*` | OSS 上传（Bitiful） |
| `QUICKER_BROWSER_HEADLESS` | 默认无头上传；设为 `false` 可显示浏览器 |

### 版本与上传策略

- `version.json` 使用**四段**版本 `M.m.b.r`（与 Release 程序集 `Name.M.m.b.r.dll` 一致）。
- `-Test`：仅第四段 +1，包目录仍为 `_packages/{packageName}/M.m.b/`。
- `-Publish` 时 qkbuild 自动选择 Quicker 上传页面：**新建包** / **追加版本** / **编辑替换**（revision 热更时替换已有 M.m.b 的 zip）。

### 双产物项目

| 目录 | 脚本 |
|------|------|
| `quicker-expression-agent/` | `build-quicker.ps1`、`build-desktop.ps1` |
| `co-detect/` | `build-server.ps1`、`build-desktop.ps1` |

## 前置

- 本机已安装 `qkbuild`（`%LOCALAPPDATA%\Programs\qkbuild` 或 PATH）
- Quicker + QuickerRpc 插件（`qkrpc` 更新子程序 `version` 变量时需要）
- 可选：仓库根 `qkref.props` / `QUICKER_DLL_PATH` 指向 Quicker 安装目录
