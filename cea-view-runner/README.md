# CeaViewRunner

Quicker 集成的 WPF net472 类库：从 `CeaQuickerTools.ViewRunner` 迁移的 Markdown 弹窗、倒计时浮窗、`ShowWindow` 参数化展示等。

## 构建

- 本地：`dotnet build CeaViewRunner.slnx -c Debug`
- Release / 打包：在仓库根目录执行 `cea-view-runner/build.ps1`（依赖 `qkbuild` 与 `build.yaml`）。
- Quicker 程序集路径：由根目录 `qkref.props` 决定，可通过环境变量 `QUICKER_DLL_PATH` 或 MSBuild `-p:QuickerDllPath=...` 覆盖。

## 版本

发布版本号维护在 `version.json` 的 `CeaViewRunner` 键；Release 输出程序集名为 `CeaViewRunner.<version>.dll`。
