# Ink Canvas Artistry 迁移到 .NET 6 计划

> 目标：将当前 `net472` 的 WPF/WinForms 桌面应用迁移至 `.NET 6 (net6.0-windows)`，并尽量保证现有功能（尤其是 PPT 联动、快捷方式启动、更新机制）行为一致。

## 1. 当前状态盘点（已确认）

- 项目当前目标框架：`net472`。  
- 同时使用 `WPF` 与 `Windows Forms`。  
- 存在 .NET Framework 专属项：
  - `App.config` 里的 `<supportedRuntime>` 与 `useLegacyV2RuntimeActivationPolicy`。  
  - `.NETFramework,Version=v4.7.2` 的 `BootstrapperPackage`。  
  - ClickOnce 风格安装属性（`Install/UpdateEnabled/...`）。
- 存在 COM/Interop 依赖：
  - `Microsoft.Office.Interop.PowerPoint`（PPT 模式）。
  - `IWshRuntimeLibrary`（开机自启快捷方式）。
  - `VBIDE` / `stdole`。
- 项目含本地 DLL 引用：`IACore.dll`、`IALoader.dll`、`IAWinFX.dll`。

## 2. 必改项（第一阶段）

### 2.1 csproj 框架与平台属性

在 `Ink Canvas/Ink Canvas.csproj` 中：

1. 将
   - `<TargetFramework>net472</TargetFramework>`
   改为
   - `<TargetFramework>net6.0-windows</TargetFramework>`。
2. 保留并确认：
   - `<UseWPF>true</UseWPF>`
   - `<UseWindowsForms>true</UseWindowsForms>`
3. 新增/确认：
   - `<EnableWindowsTargeting>true</EnableWindowsTargeting>`（在非 Windows CI 上常用，可按 CI 需求决定）。
4. 清理或调整旧 Runtime Identifier：
   - 删除 `win7*`（.NET 6 不再支持 Windows 7）。
   - 可改为 `win-x86;win-x64;win-arm64`（按实际发版需求保留）。
5. 评估并移除对 .NET Framework 残留配置：
   - `BootstrapperPackage Include=".NETFramework,Version=v4.7.2"`。
   - Framework ClickOnce 相关属性（如果不再使用原发布方式）。

### 2.2 App.config 迁移策略

`Ink Canvas/App.config` 中以下内容对 .NET 6 无效，应删除或重构：

- `<startup useLegacyV2RuntimeActivationPolicy="true">`
- `<supportedRuntime ... .NETFramework ... />`

如果仍需要应用配置，改用：

- `appsettings.json` + `Microsoft.Extensions.Configuration`（推荐）；或
- 保留最小化 `App.config`（仅用于少数兼容场景，不含 framework runtime 声明）。

### 2.3 NuGet 包与框架内置库清理

在 `csproj` 包列表中优先处理：

- 移除通常已由 .NET 6 提供的包（避免冲突或冗余）：
  - `System.Net.Http`
  - `System.IO.Compression`
  - `Microsoft.CSharp`（多数场景可省）
  - `Microsoft.VisualBasic`（若代码无依赖可去除）
- 保留并升级第三方包到支持 net6 的版本：
  - `Newtonsoft.Json`
  - `iNKORE.UI.WPF.Modern`
  - `OSVersionExt`
  - Office Interop 相关包

> 注意：包升级建议按“逐个升级 + 编译验证 + 回归验证”进行，避免一次性大改导致定位困难。

### 2.4 COM 互操作兼容性确认

`.NET 6` 支持 COM Interop，但需重点验证：

1. `Microsoft.Office.Interop.PowerPoint` 在 x86/x64 下与 Office 位数一致性。
2. `IWshRuntimeLibrary` 在 SDK-style + net6 下是否可正常生成互操作程序集。
3. `VBIDE/stdole` 是否可被正确解析并在目标机可用。

若 COMReference 在 net6 下构建不稳定，备选方案：

- 使用 NuGet 的 Interop 包替代部分 COMReference；
- 或改为手动 `tlbimp` 生成后引用（仅在必要时）。

### 2.5 本地 DLL 引用重新验证

对以下本地 DLL：

- `IACore.dll`
- `IALoader.dll`
- `IAWinFX.dll`

需要确认：

1. 是否基于 .NET Framework 编译；
2. 是否提供 netstandard / net6 兼容版本；
3. 是否存在 AppDomain、Remoting、旧 CAS 等 .NET Framework 专属 API 依赖。

若不兼容，这三者将是迁移主阻塞项。

## 3. 代码层风险检查清单（第二阶段）

建议按以下顺序执行编译修复：

1. `dotnet build` 后修复 API 变化（最小改动）。
2. 检查反射、序列化、路径/权限相关代码在 net6 行为差异。
3. 检查高 DPI、输入设备、墨迹与触控链路是否回归正常。
4. 专项回归：
   - PPT 放映联动（启动、翻页、退出）；
   - 开机自启（快捷方式创建/删除）；
   - 自动更新流程。

## 4. 发布与安装链路调整（第三阶段）

当前仓库存在 Inno Setup 打包脚本（`.iss`），迁移后建议：

1. 统一为 `self-contained` 或 `framework-dependent` 方案：
   - 教学机/离线环境通常更适合 `self-contained`。
2. 调整 Inno Setup 输入目录为 net6 发布目录（例如 `publish/win-x64`）。
3. README 中安装前置条件由“.NET Framework 4.7.2”改为“.NET Desktop Runtime 6.x”（或自包含无需安装 Runtime）。

## 5. 建议实施步骤（可执行）

1. 建立迁移分支：`feature/migrate-net6`。
2. 先仅改 `csproj` 基础框架属性与 RID（不升级业务代码）。
3. 清理 `App.config` 的 framework runtime 节点。
4. 逐个升级/移除 NuGet 包并确保可编译。
5. 验证 COM 与本地 DLL；若阻塞，先做兼容矩阵。
6. 完成功能回归后再改安装脚本与 README。
7. 预发布给小范围设备验证（x86/x64/ARM64 至少各 1 台）。

## 6. 迁移完成定义（DoD）

- `dotnet build` 在 `net6.0-windows` 下通过。
- 核心功能：书写、板擦、PPT 联动、截图、计时器、放大镜均可用。
- 安装包可在目标环境安装启动。
- README/手册中的运行时依赖描述已更新。
