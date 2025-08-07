# Taskbar Grouper

A Windows 11 utility that helps organize a cluttered taskbar by grouping related applications into custom containers. Click the system tray icon to view grouped running applications and quickly switch between them.

## Features

- **System Tray Integration**: Runs quietly in the system tray with minimal resource usage
- **Smart Grouping**: Automatically groups applications by category (Browsers, Development, Media, Office, Communication)
- **Quick Switching**: Click any application in the popup to bring it to the foreground
- **Modern UI**: Clean, Windows 11-style popup interface
- **Performance Optimized**: Lightweight and efficient with minimal system impact

## Pre-configured Groups

- **Browsers**: Chrome, Firefox, Edge, Opera, Brave
- **Development**: Visual Studio, VS Code, Rider, WebStorm, IntelliJ, Notepad++
- **Media**: VLC, Windows Media Player, Spotify, iTunes, Foobar2000
- **Office**: Word, Excel, PowerPoint, Outlook, Teams
- **Communication**: Discord, Slack, Telegram, WhatsApp, Skype

## How to Use

1. **Build and Run**: Compile the project and run the executable
2. **System Tray**: The application will appear as an icon in the system tray
3. **View Groups**: Left-click the tray icon to open the groups popup
4. **Switch Apps**: Click on any application in the popup to bring it to the foreground
5. **Context Menu**: Right-click the tray icon for additional options

## Requirements

- Windows 10/11
- .NET 6.0 or later
- Administrative privileges may be required for some window management operations

## Technical Details

- **Framework**: WPF with .NET 6
- **Architecture**: Service-based with separation of concerns
- **Window Management**: Native Windows API calls for efficient window enumeration and switching
- **UI**: Modern, responsive design with smooth animations

## Building

```bash
dotnet build
dotnet run
```

## Future Enhancements

- Custom group configuration
- Group color customization
- Keyboard shortcuts
- Auto-start with Windows
- Group export/import
- Advanced filtering options

## License

This project is provided as-is for educational and personal use.
