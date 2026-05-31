# 创建发布版本 – MidiController

本指南介绍如何在 GitHub 上发布新版本。
有两种方式：**通过 GitHub Actions 自动发布**（推荐）或**手动发布**。

---

## 目录

- [准备工作](#准备工作)
- [方式 A：通过 GitHub Actions 自动发布](#方式-a通过-github-actions-自动发布)
- [方式 B：手动发布（不使用 GitHub Actions）](#方式-b手动发布不使用-github-actions)
- [配置 GitHub Actions 工作流](#配置-github-actions-工作流)
- [发布检查清单](#发布检查清单)
- [版本命名规范](#版本命名规范)

---

## 准备工作

1. **确定版本号** – 按照 `v0.3.0` 格式确定新版本号（见[版本命名规范](#版本命名规范)）。

2. **更新 CHANGELOG**（如有）：
   ```markdown
   ## [0.3.0] – 2025-01-15
   ### 新增
   - 完整触发器编辑器
   - 预/后阶段的 MIDI 输出
   - 单 EXE 自包含发布
   ```

3. **分支为 `main` 且所有更改已提交：**
   ```powershell
   git status          # 无未提交的更改
   git log --oneline -5
   ```

4. **本地验证构建：**
   ```powershell
   dotnet build Midi2InputDevice.slnx -c Release
   dotnet test
   ```

5. **测试发布：**
   ```powershell
   dotnet publish MidiController.Frontend\MidiController.Frontend.csproj `
	 /p:PublishProfile=SingleFile -c Release

   # 验证 EXE 存在：
   dir MidiController.Frontend\bin\Release\publish\
   # 应包含 MidiController.Frontend.exe
   ```

---

## 方式 A：通过 GitHub Actions 自动发布

### 步骤 1 – 配置 GitHub Actions 工作流

如果尚未配置：[配置 GitHub Actions 工作流](#配置-github-actions-工作流)。

### 步骤 2 – 创建并推送 Git 标签

```powershell
# 本地创建标签
git tag v0.3.0 -m "Release v0.3.0: 完整触发器编辑器、MIDI 输出、单 EXE"

# 推送标签到 GitHub（触发 Actions 工作流）
git push origin v0.3.0
```

### 步骤 3 – 监控 GitHub Actions

1. 在 GitHub 上打开仓库。
2. 进入 **Actions** 选项卡 → 点击正在运行的 **"Build & Release"** 工作流。
3. 完成后，发布版本会自动出现在 **Releases** 下。

### 步骤 4 – 在 GitHub 上验证发布

1. 在 GitHub 上打开 **Releases** 选项卡。
2. 点击新发布版本。
3. 检查：
   - 标签和标题正确（如 `v0.3.0`）
   - 文件附件 `MidiController-v0.3.0-win-x64.zip` 存在
   - ZIP 包含 `MidiController.Frontend.exe`、`appsettings.json`、`appsettings.backend.json`

4. 可选：手动编辑发布描述（点击 **Edit**）。

---

## 方式 B：手动发布（不使用 GitHub Actions）

### 步骤 1 – 创建单文件 EXE

```powershell
cd C:\Users\larsh\source\repos\danroyton\Midi2InputDevice

dotnet publish MidiController.Frontend\MidiController.Frontend.csproj `
  /p:PublishProfile=SingleFile -c Release
```

输出：`MidiController.Frontend\bin\Release\publish\`

### 步骤 2 – 打包发布 ZIP

```powershell
$version = "v0.3.0"
$publishDir = "MidiController.Frontend\bin\Release\publish"
$zipName    = "MidiController-$version-win-x64.zip"

Compress-Archive -Force `
  -Path "$publishDir\MidiController.Frontend.exe", `
		"$publishDir\appsettings.json", `
		"$publishDir\appsettings.backend.json" `
  -DestinationPath $zipName

Write-Host "ZIP 已创建：$zipName"
```

### 步骤 3 – 创建并推送 Git 标签

```powershell
git tag v0.3.0 -m "Release v0.3.0"
git push origin v0.3.0
```

### 步骤 4 – 在 GitHub 上创建发布

1. 打开 GitHub → 仓库 → **Releases** 选项卡 → **[Draft a new release]**
2. **"Choose a tag"**：选择标签 `v0.3.0`。
3. 输入**发布标题**，例如：
   `v0.3.0 – 完整触发器编辑器、MIDI 输出、单 EXE`
4. 输入**描述**（Markdown）：
   ```markdown
   ## v0.3.0 新增内容

   - ✅ 完整触发器编辑器（预/后赋值、条件块、ELSE 分支）
   - ✅ 预/后阶段的 MIDI 输出（NoteOn、NoteOff、CC、ProgramChange、PitchBend）
   - ✅ 单 EXE 自包含发布（无需安装 .NET）
   - ✅ 系统托盘仅在实际 MIDI 活动时闪烁
   - ✅ 键盘测试视图

   ## 安装方法

   1. 下载并解压 ZIP
   2. 启动 `MidiController.Frontend.exe`
   3. 无需安装程序，无需 .NET 运行时

   ## 注意事项

   - 需要 Windows 10/11（64 位）
   - 首次启动时如出现 SmartScreen 警告，请确认运行
   ```
5. **"Attach binaries"**：上传之前创建的 ZIP 文件。
6. 启用**"Set as the latest release"**。
7. 点击 **[Publish release]**。

---

## 配置 GitHub Actions 工作流

如果 `.github/workflows/release.yml` 尚不存在，请创建此文件：

**文件路径：** `.github/workflows/release.yml`

```yaml
name: Build & Release

on:
  push:
	tags:
	  - 'v*.*.*'

jobs:
  build-and-release:
	runs-on: windows-latest

	steps:
	  - name: Checkout
		uses: actions/checkout@v4

	  - name: 配置 .NET 10 SDK
		uses: actions/setup-dotnet@v4
		with:
		  dotnet-version: '10.0.x'

	  - name: 还原依赖项
		run: dotnet restore Midi2InputDevice.slnx

	  - name: 构建
		run: dotnet build Midi2InputDevice.slnx -c Release --no-restore

	  - name: 运行测试
		run: dotnet test Midi2InputDevice.slnx -c Release --no-build --verbosity normal

	  - name: 单文件发布
		run: |
		  dotnet publish MidiController.Frontend/MidiController.Frontend.csproj `
			/p:PublishProfile=SingleFile -c Release

	  - name: 创建发布 ZIP
		shell: pwsh
		run: |
		  $tag     = "${{ github.ref_name }}"
		  $pubDir  = "MidiController.Frontend/bin/Release/publish"
		  $zipName = "MidiController-$tag-win-x64.zip"
		  Compress-Archive -Force `
			-Path "$pubDir/MidiController.Frontend.exe", `
				  "$pubDir/appsettings.json", `
				  "$pubDir/appsettings.backend.json" `
			-DestinationPath $zipName
		  echo "ZIP_NAME=$zipName" >> $env:GITHUB_ENV

	  - name: 创建 GitHub 发布并上传 ZIP
		uses: softprops/action-gh-release@v2
		with:
		  files: ${{ env.ZIP_NAME }}
		  generate_release_notes: true
		env:
		  GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

### 激活工作流

```powershell
git add .github/workflows/release.yml
git commit -m "ci: 添加 GitHub Actions 发布工作流"
git push origin main
```

此后，每个匹配 `v*.*.*` 的标签都会触发自动构建和发布。

---

## 发布检查清单

每次发布前请逐项检查：

- [ ] `main` 分支已是最新（`git pull`）
- [ ] 无未提交的更改（`git status`）
- [ ] 构建成功（`dotnet build -c Release`）
- [ ] 测试通过（`dotnet test`）
- [ ] 发布成功，EXE 可在本地启动
- [ ] CHANGELOG / 发布描述已更新
- [ ] Git 标签已创建并推送
- [ ] 在 GitHub 上验证发布（附件存在，下载链接可用）
- [ ] README.md 显示正确的当前版本

---

## 版本命名规范

本项目使用[语义化版本](https://semver.org/lang/zh-CN/)：

```
v 主版本号 . 次版本号 . 修订号
  │           │          └── 问题修复 / 小修正
  │           └──────────── 新功能，向后兼容
  └──────────────────────── 破坏性 API 更改 / 重大重构
```

示例：
- `v0.3.0` → 新功能版本（触发器编辑器）
- `v0.3.1` → v0.3.0 的问题修复
- `v0.4.0` → 下一个功能版本（虚拟 MIDI 端口）
- `v1.0.0` → 第一个稳定的 Windows 服务版本

标签始终使用 `v0.3.0` 格式（带 `v` 前缀）——Actions 工作流期望此格式。
