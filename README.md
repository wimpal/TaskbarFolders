# Taskbar Grouper

Taskbar Grouper is a modern Windows 11 utility that provides a popup window above your taskbar button, showing all open application windows with their icons and titles. You can quickly switch between running apps by clicking their entries in the popup. The design matches the Windows 11 look and feel, with rounded corners, drop shadows, and dynamic theming.

## Features
- Popup appears above the taskbar button when clicked
- Lists all open windows with icons and titles
- Click an entry to bring that window to the foreground
- Modern Windows 11 styling (rounded corners, drop shadow, accent colors)
- Only one popup can be open at a time
- Popup auto-sizes to its content
- Single-instance application (only one can run at a time)

## Getting Started

### Prerequisites
- Windows 10/11
- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- Visual Studio Code (recommended) or Visual Studio

### Build & Run

#### Using VS Code
1. **Clone the repository**
2. **Open the folder in VS Code**
3. Press `Ctrl+Shift+P` → "Tasks: Run Task" → select `Stop and Run TaskbarGrouper` (recommended)
   - This will stop any running instance and launch the app
4. Click the Taskbar Grouper icon in your taskbar to show/hide the popup

#### Using Command Line
```powershell
# Stop any running instance (optional, but recommended)
Get-Process -Name 'TaskbarGrouper' -ErrorAction SilentlyContinue | Stop-Process -Force

# Build
 dotnet build TaskbarGrouper.csproj

# Run
 dotnet run --project TaskbarGrouper.csproj
```

## Usage
- Click the Taskbar Grouper icon in your taskbar to open the popup
- Click again to close it
- Click any app in the popup to bring it to the foreground

## Troubleshooting
- If you see errors about `InitializeComponent` or `AppListPanel` not existing, try cleaning and rebuilding the project:
  ```powershell
  dotnet clean TaskbarGrouper.csproj
  dotnet build TaskbarGrouper.csproj
  ```
- If the popup doesn't appear, make sure you are running only one instance and that your .NET SDK is up to date.

## License
MIT
