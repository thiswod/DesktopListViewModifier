# DesktopListViewModifier

A Windows desktop icon view modifier tool that allows you to easily switch between different desktop views (Details view and Large icons view).

## Language
- [中文](README.md) - 中文版README
- [English](README_EN.md) - English version README

## Features

✅ **View Switching**: Quickly switch between Details view and Large icons view with one click
✅ **Auto-refresh**: Automatically refreshes the desktop after changing views
✅ **Explorer Restart**: Automatically restarts Windows Explorer to apply view changes
✅ **Registry Backup**: Backs up and restores desktop view settings
✅ **Admin Support**: Includes administrator privilege handling for system-level modifications

## Screenshots

### Windows 11 Context Menu
![Windows 11 Context Menu](img/Snipaste_2025-10-18_14-38-04.png)

### Classic Context Menu
![Classic Context Menu](img/Snipaste_2025-10-18_14-38-26.png)

## Usage

1. Download the latest release from the [Releases](https://github.com/thiswod/DesktopListViewModifier/releases) page
2. Run the program with administrator privileges (Windows may require admin rights to modify desktop settings)
3. Click the "Set Details View" button to switch to Details view
4. Click the "Set Large Icons View" button to switch to Large icons view

## Technical Implementation

The tool interacts with Windows Explorer's SysListView32 control through Windows API calls to modify desktop view settings. Key implementations include:

- **Win32 API Integration**: Uses FindWindowEx, SendMessage, and EnumWindows functions
- **Registry Manipulation**: Stores view settings in the Windows Registry
- **Explorer Process Management**: Handles Explorer restart to apply changes
- **ListView Control Interaction**: Modifies ListView styles and attributes

## Compilation Instructions

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or later (recommended)

### Command Line Compilation

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build --configuration Release

# Publish the application
dotnet publish -c Release -r win-x64 --self-contained true
```

### Building with Visual Studio
1. Open DesktopListViewModifier.sln in Visual Studio
2. Set the build configuration to Release
3. Select Build > Build Solution

## Notes

- This tool modifies system-level settings and may trigger Windows security warnings
- Effects may vary slightly across different Windows versions
- Windows 11 and Windows 10 are officially supported

## View Persistence Solution

✅ **Persistence Solution**: Setting Details view manually via right-click ensures the setting persists after Explorer restarts and system reboots.

### Setting Steps:
1. Right-click on an empty area of the desktop
2. Select "View" → "Details"
3. After this setting, the desktop view will remain in Details mode even after Explorer restarts or system reboots

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Disclaimer

This tool is provided as-is without any warranty. Use at your own risk. The author is not responsible for any system damage or data loss caused by using this tool.