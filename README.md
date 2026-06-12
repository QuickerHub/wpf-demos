# wpf-demos

> **主仓库已迁移至 [quicker-workspace](https://github.com/QuickerHub/quicker-workspace)**  
> 28 个 WPF/DLL 依赖包现位于 `quicker-workspace/packages/`（分级 monorepo）。本仓库仅保留尚未迁入的实验性/遗留项目。

## 仍在 wpf-demos 的项目

| 目录 | 说明 |
|------|------|
| `co-detect/` | 双产物（desktop + server），体积大 |
| `sync-mvp/` | 同步方案 MVP |
| `text-process/` | 文本处理 |
| `example/` | net472 脚手架模板 |
| `quicker-modifier/`、`quicker-reptile-tools/`、`word-control/` | 遗留/无 qkbuild |

## 新开发入口

```powershell
# 克隆工作区
git clone https://github.com/QuickerHub/quicker-workspace.git
cd quicker-workspace/packages/platform/expression-enhanced
.\build.ps1 -Test
```

构建说明、skills、子程序约定见 [quicker-workspace README](https://github.com/QuickerHub/quicker-workspace/blob/main/README.md)。

## co-detect 构建（仍在本仓库）

```powershell
cd co-detect
.\build-desktop.ps1    # 或 build-server.ps1
```

依赖根目录 `scripts/Invoke-Qkbuild.ps1` 与 `qkref.props`。

## 前置

- [qkbuild](https://github.com/QuickerHub/quicker_build_net) 已安装
- Quicker + QuickerRpc 插件（更新子程序 `version` 时需要）
