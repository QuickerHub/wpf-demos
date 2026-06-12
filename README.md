# wpf-demos

> **已归档。** WPF/DLL 主开发在 [quicker-workspace](https://github.com/QuickerHub/quicker-workspace)；语言检测在 [co-detect](https://github.com/QuickerHub/co-detect)。

本仓库仅保留少量遗留/模板目录，**不再维护构建脚本**。

## 剩余目录

| 目录 | 说明 |
|------|------|
| `example/` | net472 脚手架示例（依赖根目录 `qkref.props`） |
| `quicker-reptile-tools/` | 遗留小工具 |
| `word-control/` | Word 自动化实验 |

## 新开发请用

```powershell
git clone https://github.com/QuickerHub/quicker-workspace.git
cd quicker-workspace/packages/platform/expression-enhanced
.\build.ps1 -Test
```

规范与 skills 见 [quicker-workspace](https://github.com/QuickerHub/quicker-workspace)。
