# DesktopListViewModifier

## 语言
- [中文](README.md) - 中文版README
- [English](README_EN.md) - English version README

一个用于修改Windows桌面图标视图模式的工具，可以将桌面图标切换为详细信息视图，并支持恢复默认的大图标视图。

## 功能特点

- ✅ 将桌面图标视图切换为详细信息模式，方便查看文件详情
- ✅ 一键恢复桌面图标为默认大图标视图
- ✅ 自动刷新桌面，无需手动刷新即可看到效果
- ✅ 支持Windows 10/11系统
- ✅ 简洁的用户界面，操作直观

## 界面预览

### Windows 11 右键菜单
![Windows 11 右键菜单](img/Snipaste_2025-10-18_14-38-04.png)

### 经典右键菜单
![经典右键菜单](img/Snipaste_2025-10-18_14-38-26.png)

应用程序提供了两个主要按钮：
- **设置为详细信息视图** - 将桌面图标排列方式切换为详细信息模式
- **恢复大图标视图** - 将桌面图标恢复为系统默认的大图标显示方式

## 使用方法

1. 下载或编译程序
2. 以管理员权限运行程序（Windows可能需要管理员权限才能修改桌面设置）
3. 点击相应按钮进行视图切换
4. 程序会自动刷新桌面，您可以立即看到效果

## 技术原理

本工具通过Windows API直接与桌面的ListView控件进行交互：

1. 使用`FindWindow`和`FindWindowEx`找到桌面的SysListView32控件
2. 通过`SendMessage`发送`LVM_SETVIEW`消息修改视图模式
3. 使用多种刷新技术确保视图更新正确显示：
   - 发送系统通知消息
   - 强制重绘窗口
   - 模拟F5键刷新

## 编译说明

### 环境要求

- [.NET 8.0 SDK 或更高版本](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)
- Visual Studio 2022 或兼容的IDE
- Windows操作系统

### 编译步骤

1. 克隆或下载项目代码
2. 打开`DesktopListViewModifier.sln`解决方案文件
3. 选择目标平台（x86或x64）
4. 构建解决方案（Build Solution）

### 命令行编译

```bash
# 进入项目目录
cd DesktopListViewModifier

# 编译项目
dotnet build -c Release

# 发布项目（可选）
dotnet publish -c Release -r win-x64 --self-contained true
```

## 注意事项

- 某些Windows版本或个性化设置可能会影响工具的效果
- 程序需要以足够的权限运行才能修改桌面设置
- 桌面视图模式更改可能在某些系统操作后自动恢复
- 效果可能因不同Windows版本而略有差异

## 视图持久化解决方案

✅ **持久化问题解决方法**：通过手动右键设置详细信息显示可以确保设置在重启资源管理器和系统后保持不变。

### 设置步骤：
1. 在桌面空白处右键点击
2. 选择「查看」→「详细信息」
3. 这样设置后，即使重启资源管理器或系统，桌面视图也会保持为详细信息模式

## 许可证

本项目为开源软件，仅供个人学习和研究使用。

## 免责声明

使用本工具时请谨慎操作。作者不对因使用本工具导致的任何系统问题或数据丢失负责。请在使用前备份重要数据。