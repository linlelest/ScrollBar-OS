# ScrollBar OS

<div align="center">


**轻量级 Windows 桌面增强工具 - 胶囊式窗口管理器**

[![Build Status](https://github.com/linlelest/ScrollBar-OS/actions/workflows/build.yml/badge.svg)](https://github.com/linlelest/ScrollBar-OS/actions)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey)]()
[![.NET](https://img.shields.io/badge/.NET-8.0-purple)]()

</div>

---

## 📖 项目简介

ScrollBar OS 是一款基于 **WinUI 3** 和 **.NET 8** 开发的 Windows 桌面增强工具。它以独特的**胶囊形态**悬浮于屏幕边缘，提供窗口快速切换、平铺布局、系统监控、任务栏隐藏等功能，旨在替代传统任务栏，打造极简、高效、流畅的桌面交互体验。

### 核心特性

- 🎯 **胶囊式 UI** - 垂直胶囊设计，占屏幕高度 33%，支持左/右侧停靠
- 🖱️ **智能滚动切换** - 慢速滚动直接切换窗口，快速滚动进入列表模式
- 🪟 **窗口平铺** - 拖拽组合网格，一键平铺所有窗口
- 📊 **硬件监控** - 实时显示 CPU、内存、磁盘、网络使用率
- 🎨 **Fluent 设计** - 支持 Mica/Acrylic 材质，圆角、动画、高 DPI 适配
- ⚡ **轻量高效** - Self-contained 部署，无需安装运行时，内存占用 < 50MB
- 🔧 **高度可配置** - 6 项核心设置，JSON 配置热重载

---

## ✨ 功能详解

### 1. 胶囊主体

| 区域 | 功能 |
|:---|:---|
| 上部滚动区 | 应用图标流式列表，悬浮显示 Tooltip |
| 下部固定区 | 小组件 + 3个微型固定应用 + 设置按钮 |

- 图标默认 36×36，圆角 8px
- 悬浮时图标放大 1.15 倍（弹性动画）
- 右键点击图标可固定到快速启动

### 2. 滚动交互

- **慢速滚动** (200ms 内 ≤3 格)：实时切换聚焦窗口
- **快速滚动** (200ms 内 >3 格)：触发文本列表模式
  - 列表支持惯性滚动
  - 停止后高亮当前项
  - 1.5 秒后自动退出并聚焦窗口

### 3. 窗口平铺

- 从胶囊拖出图标触发组合网格
- 网格支持自由拖入/拖出/交换
- 平铺算法：`cols = ceil(sqrt(N))`，等分工作区
- 支持 4 种布局：Grid / Horizontal / Vertical / MasterSlave
- `Ctrl+Z` 撤销上次平铺

### 4. 系统交互

- **任务栏隐藏**：`Win+T` 快捷键切换
- **托盘桥接**：显示最小化应用，点击恢复
- **硬件监控**：2 秒间隔采集，悬浮触发 Popup
- **全局热键**：支持自定义快捷键

### 5. 设置项

| 设置 | 说明 |
|:---|:---|
| 胶囊背景 | 材质选择 (Solid/Gradient/Mica/Acrylic) + 透明度 |
| 胶囊位置 | 左侧 / 右侧 |
| 小组件 | 日期时间、硬件监控开关 |
| 隐藏任务栏 | Toggle 开关 |
| 语言 | 简体中文 / English |
| 组件大小 | 胶囊宽度、图标尺寸滑块 |

---

## 🏗️ 技术架构

### 技术栈

| 组件 | 技术选型 |
|:---|:---|
| 语言 | C# 12 |
| 框架 | .NET 8 |
| UI | WinUI 3 (Windows App SDK 1.7) |
| 架构模式 | MVVM (CommunityToolkit.Mvvm) |
| Win32 API | CsWin32 源生成器 |
| 配置存储 | System.Text.Json |
| 部署 | Self-contained + Trimming |

### 项目结构

```
ScrollBar-OS/
├── .github/workflows/
│   └── build.yml                 # GitHub Actions CI/CD
├── src/ScrollBarOS/
│   ├── App.xaml / App.xaml.cs    # 应用入口
│   ├── MainWindow.xaml / .cs     # 主窗口（透明、置顶、点击穿透）
│   ├── app.manifest              # DPI 感知、管理员权限
│   ├── NativeMethods.txt         # CsWin32 API 声明
│   ├── Models/                   # 数据模型
│   │   ├── WindowInfo.cs
│   │   ├── AppConfig.cs
│   │   ├── HardwareInfo.cs
│   │   └── PinnedAppInfo.cs
│   ├── ViewModels/               # 视图模型
│   ├── Views/                    # UI 视图
│   │   ├── CapsuleControl.xaml   # 胶囊主控件
│   │   ├── SettingsPanel.xaml    # 设置面板
│   │   ├── TilingGrid.xaml       # 平铺网格
│   │   ├── WindowListOverlay.xaml# 窗口列表
│   │   ├── HardwarePopup.xaml    # 硬件弹窗
│   │   └── TrayMenu.xaml         # 托盘菜单
│   ├── Services/                 # 业务服务
│   │   ├── WindowService.cs      # 窗口枚举/切换
│   │   ├── ScrollStateMachine.cs # 滚动状态机
│   │   ├── TilingService.cs      # 平铺算法
│   │   ├── HardwareMonitorService.cs
│   │   ├── TaskbarService.cs
│   │   ├── TrayService.cs
│   │   ├── ConfigService.cs
│   │   └── HotkeyService.cs
│   ├── Helpers/                  # 辅助工具
│   │   ├── Win32Helper.cs
│   │   └── DpiHelper.cs
│   ├── Converters/               # XAML 转换器
│   └── Strings/                  # 多语言资源
│       ├── zh-CN/Resources.resw
│       └── en-US/Resources.resw
├── ScrollBarOS.sln
├── .gitignore
└── README.md
```

### 架构设计

```
┌─────────────────────────────────────────────────────────────┐
│                      UI 渲染层 (前台线程)                      │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │ CapsuleControl│ │SettingsPanel│  │  TilingGrid/Overlay │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
├─────────────────────────────────────────────────────────────┤
│                    状态管理层 (IMessenger)                     │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │  AppConfig  │  │ WindowList  │  │   HardwareInfo      │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
├─────────────────────────────────────────────────────────────┤
│                   系统交互层 (后台线程池)                       │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │WindowService│  │TaskbarService│ │ HardwareMonitor     │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │ TrayService │  │HotkeyService│  │   TilingService     │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## 🚀 快速开始

### 系统要求

- Windows 10 版本 1809 (Build 17763) 或更高版本
- Windows 11 推荐
- x64 或 ARM64 架构

### 下载安装

1. 前往 [Releases](https://github.com/linlelest/ScrollBar-OS/releases) 页面
2. 下载最新版本的 `ScrollBarOS-win-x64.zip`
3. 解压到任意目录
4. 运行 `ScrollBarOS.exe`

> **注意**：首次运行需要管理员权限（用于隐藏任务栏等功能）

### 从源码构建

本项目使用 **GitHub Actions** 进行自动化构建，本地无需安装编译环境。

#### 方法一：GitHub Actions（推荐）

1. Fork 本仓库
2. 推送代码触发自动构建
3. 在 Actions 页面下载构建产物

#### 方法二：本地构建（需要 Visual Studio 2022）

```bash
# 克隆仓库
git clone https://github.com/linlelest/ScrollBar-OS.git
cd ScrollBar-OS

# 还原依赖
dotnet restore ScrollBarOS.sln

# 构建
dotnet build ScrollBarOS.sln -c Release -p:Platform=x64

# 发布（Self-contained）
dotnet publish src/ScrollBarOS/ScrollBarOS.csproj -c Release -r win-x64 --self-contained true -o ./publish
```

---

## ⌨️ 快捷键

| 快捷键 | 功能 |
|:---|:---|
| `Win + T` | 切换任务栏显示/隐藏 |
| `Ctrl + Z` | 撤销上次窗口平铺 |
| 鼠标滚轮（慢速） | 切换聚焦窗口 |
| 鼠标滚轮（快速） | 打开窗口列表 |
| 右键点击图标 | 固定到快速启动 |

---

## ⚙️ 配置说明

配置文件位置：`%APPDATA%\ScrollBarOS\config.json`

```json
{
  "backgroundColor": "#CC1E1E2E",
  "material": "Acrylic",
  "capsulePosition": "Right",
  "capsuleWidth": 64,
  "capsuleHeightPercent": 0.33,
  "iconSize": 36,
  "fontSize": 12,
  "showDateTimeWidget": false,
  "showHardwareWidget": true,
  "hideTaskbar": false,
  "language": "Chinese",
  "pinnedApps": [],
  "startWithWindows": false,
  "scrollThreshold": 3,
  "cornerRadius": 20,
  "backgroundOpacity": 0.8
}
```

---

## 📦 构建与发布

### GitHub Actions 工作流

- **触发条件**：推送到 `main`/`master` 分支，或创建 `v*` 标签
- **构建环境**：`windows-latest`
- **输出产物**：
  - `ScrollBarOS-win-x64.zip` - x64 便携版
- **Release 发布**：推送 `v*` 标签自动创建 GitHub Release

### 构建优化

- ✅ Self-contained 部署（无需 .NET 运行时）
- ✅ IL Trimming（裁剪未使用代码）
- ✅ 单文件输出
- ✅ 构建缓存加速

---

## 🛣️ 开发路线图

### v1.0 (当前版本)
- [x] 胶囊 UI 基础框架
- [x] 窗口枚举与切换
- [x] 滚动状态机
- [x] 窗口平铺算法
- [x] 任务栏隐藏
- [x] 硬件监控
- [ ] 设置面板
- [x] 多语言支持
- [x] GitHub Actions CI/CD

### v1.1 (计划中)
- [ ] 多显示器完整支持
- [ ] 虚拟桌面集成
- [ ] 窗口分组/标签
- [ ] 自定义主题
- [ ] 插件系统

### v2.0 (远期)
- [ ] NativeAOT 编译（待 WinUI 3 完善支持）
- [ ] AI 窗口管理建议
- [ ] 云配置同步
- [ ] 社区插件市场

---

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request！

### 开发环境

- Visual Studio 2022 17.8+（安装 "Windows 应用开发" 工作负载）
- .NET 8 SDK
- Windows App SDK 1.7

### 代码规范

- 遵循 C# 12 语言规范
- 使用 MVVM 架构模式
- 所有 Win32 调用通过 CsWin32 或 Win32Helper 封装
- UI 更新必须通过 DispatcherQueue

---

## 📄 许可证

本项目采用 [MIT 许可证](LICENSE)。

---

## 🙏 致谢

- [WinUI 3](https://learn.microsoft.com/windows/apps/winui/winui3/) - 现代 Windows UI 框架
- [Windows App SDK](https://learn.microsoft.com/windows/apps/windows-app-sdk/) - Windows 应用开发套件
- [CommunityToolkit](https://github.com/CommunityToolkit) - MVVM 工具包
- [CsWin32](https://github.com/microsoft/CsWin32) - Win32 API 源生成器

---

<div align="center">

**Made with ❤️ for Windows Desktop Enhancement**

[报告问题](https://github.com/linlelest/ScrollBar-OS/issues) · [功能建议](https://github.com/linlelest/ScrollBar-OS/issues/new) · [参与讨论](https://github.com/linlelest/ScrollBar-OS/discussions)

</div>
