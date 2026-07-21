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
- 🪟 **窗口平铺** - 独立平铺窗口，支持 4 种布局模式
- 📊 **硬件监控** - 实时显示 CPU、内存、磁盘、网络使用率
- 🎨 **Fluent 设计** - 支持 Mica/Acrylic 材质，圆角、动画、高 DPI 适配
- ⚡ **轻量高效** - Self-contained 部署，无需安装运行时
- 🔧 **高度可配置** - JSON 配置热重载，支持多种自定义选项
- 📌 **固定应用** - 支持固定常用应用到快速启动区

---

## ✨ 功能详解

### 1. 胶囊主体

| 区域 | 功能 |
|:---|:---|
| 上部滚动区 | 应用图标流式列表，悬浮显示 Tooltip |
| 下部固定区 | 日期时间小组件 + 硬件监控 + 平铺按钮 + 设置按钮 + 关闭按钮 |

- 图标默认 44×44，圆角 8px
- 悬浮时图标放大 1.15 倍（弹性动画）
- 右键点击图标可固定到快速启动
- 点击图标切换窗口，已聚焦窗口点击则最小化

### 2. 滚动交互

- **慢速滚动** (200ms 内 ≤3 格)：实时切换聚焦窗口
- **快速滚动** (200ms 内 >3 格)：触发文本列表模式
  - 列表支持惯性滚动
  - 停止后高亮当前项
  - 1.5 秒后自动退出并聚焦窗口

### 3. 窗口平铺

- 点击平铺按钮打开独立平铺配置窗口
- 显示当前可见窗口的网格预览
- 支持从胶囊拖入窗口图标
- 支持删除窗口项
- 4 种布局模式：
  - **Grid**：网格布局，自动计算行列
  - **Horizontal**：水平分割
  - **Vertical**：垂直分割
  - **MasterSlave**：主从布局
- 点击确认按钮执行平铺

### 4. 系统交互

- **任务栏隐藏**：设置中开启/关闭
- **硬件监控**：2 秒间隔采集，显示 CPU、内存、磁盘、网络使用率
- **固定应用**：右键点击窗口图标可固定到快速启动区
- **启动应用**：点击固定应用图标启动程序

### 5. 设置项

| 设置 | 说明 |
|:---|:---|
| 胶囊位置 | 左侧 / 右侧 |
| 背景材质 | Solid / Acrylic / Mica |
| 背景透明度 | 20% - 100% |
| 胶囊宽度 | 48px - 96px |
| 图标尺寸 | 24px - 48px |
| 硬件监控 | 开关 |
| 日期时间 | 开关 |
| 隐藏任务栏 | 开关 |
| 滚动阈值 | 快速滚动触发阈值（默认 3） |

---

## 🏗️ 技术架构

### 技术栈

| 组件 | 技术选型 |
|:---|:---|
| 语言 | C# 12 |
| 框架 | .NET 8 |
| UI | WinUI 3 (Windows App SDK) |
| 架构模式 | Service-based |
| Win32 API | P/Invoke |
| 配置存储 | System.Text.Json |
| 部署 | Self-contained |

### 项目结构

```
ScrollBar-OS/
├── src/
│   └── ScrollBarOS/
│       ├── MainWindow.xaml          # 主窗口 XAML 定义
│       ├── MainWindow.xaml.cs       # 主窗口逻辑
│       ├── SettingsWindow.cs        # 设置窗口（纯代码 UI）
│       ├── TilingWindow.cs          # 平铺配置窗口
│       ├── Models/                  # 数据模型
│       │   ├── AppConfig.cs         # 应用配置
│       │   ├── HardwareInfo.cs      # 硬件信息
│       │   ├── PinnedAppInfo.cs     # 固定应用信息
│       │   └── WindowInfo.cs        # 窗口信息
│       ├── Services/                # 服务层
│       │   ├── ConfigService.cs     # 配置管理
│       │   ├── HardwareMonitorService.cs  # 硬件监控
│       │   ├── ScrollStateMachine.cs      # 滚动状态机
│       │   ├── TaskbarGuard.cs      # 任务栏守护
│       │   ├── TaskbarService.cs    # 任务栏控制
│       │   ├── TilingService.cs     # 窗口平铺
│       │   ├── TrayService.cs       # 托盘和固定应用
│       │   └── WindowService.cs     # 窗口管理
│       ├── Helpers/                 # 辅助工具
│       │   ├── DpiHelper.cs         # DPI 适配
│       │   └── Win32Helper.cs       # Win32 API 封装
│       ├── Converters/              # 值转换器
│       └── Strings/                 # 本地化资源
├── global.json                      # .NET 版本配置
└── README.md
```

### 核心服务

- **WindowService**：窗口枚举、聚焦、最小化、图标提取
- **ScrollStateMachine**：滚动状态管理，区分慢速/快速滚动
- **TilingService**：窗口平铺算法，支持多种布局模式
- **HardwareMonitorService**：硬件指标采集（CPU、内存、磁盘、网络）
- **ConfigService**：配置持久化，支持热重载
- **TaskbarService**：任务栏隐藏/显示
- **TrayService**：固定应用管理
```
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

### 从源码构建

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
| 鼠标滚轮（慢速） | 切换聚焦窗口 |
| 鼠标滚轮（快速） | 打开窗口列表 |
| 右键点击图标 | 固定到快速启动 |
| 点击图标 | 切换窗口（已聚焦则最小化） |

---

## ⚙️ 配置说明

配置文件位置：`%APPDATA%\ScrollBarOS\config.json`

```json
{
  "material": "Acrylic",
  "capsulePosition": "Right",
  "capsuleWidth": 64,
  "capsuleHeightPercent": 0.33,
  "iconSize": 32,
  "showDateTimeWidget": true,
  "showHardwareWidget": true,
  "hideTaskbar": false,
  "pinnedApps": [],
  "startWithWindows": false,
  "scrollThreshold": 3,
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

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request！

### 开发环境

## 🛠️ 开发环境

### 环境要求

- .NET 8 SDK
- Windows App SDK
- Visual Studio（可选）

### 代码规范

- 遵循 C# 12 语言规范
- 使用 Service-based 架构模式
- 所有 Win32 调用通过 Win32Helper 封装（P/Invoke）
- UI 更新必须通过 Dispatcher

---

## 📄 许可证

本项目采用 [MIT 许可证](LICENSE)。

---

## 🙏 致谢

- [WinUI 3](https://learn.microsoft.com/windows/apps/winui/winui3/) - 现代 Windows UI 框架
- [Windows App SDK](https://learn.microsoft.com/windows/apps/windows-app-sdk/) - Windows 应用开发套件
- [.NET 8](https://learn.microsoft.com/dotnet/core/) - 跨平台开发框架

---

<div align="center">

**Made with ❤️ for Windows Desktop Enhancement**

[报告问题](https://github.com/linlelest/ScrollBar-OS/issues) · [功能建议](https://github.com/linlelest/ScrollBar-OS/issues/new) · [参与讨论](https://github.com/linlelest/ScrollBar-OS/discussions)

</div>
