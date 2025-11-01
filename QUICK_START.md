# UAInspector - Quick Start Guide

## ?? What's Been Built

Your UAInspector application now has a complete **UI framework** and **MVVM architecture** ready to go!

### ? Completed Components

1. **Modern Fluent Design UI**
   - Dark theme with Microsoft color scheme
   - Smooth animations and hover effects
   - Rounded corners and modern styling
   - Responsive layout

2. **MVVM Architecture**
   - `ViewModelBase` - Base class with INotifyPropertyChanged
   - `RelayCommand` - Command implementation
   - `MainViewModel` - Main window logic
   - `ServerListViewModel` - Server management logic

3. **Views**
   - `MainWindow` - Main application window with navigation
   - `ServerListView` - Server list and discovery UI
   - Beautiful card-based layout

4. **Models**
   - `OpcServerInfo` - Server information
   - `OpcNodeInfo` - OPC UA node information
   - `AppSettings` - Application settings
   - `LoginType` enum - Authentication types

5. **Services**
   - `StorageService` - Save/load servers, settings, favorites using XML

6. **Resources**
   - Complete style dictionary with modern colors and controls

## ?? Running the Application

1. **Build the Solution**
   ```
   Press F6 or Build ? Build Solution
   ```

2. **Run the Application**
   ```
   Press F5 or Debug ? Start Debugging
   ```

3. **What You'll See**
   - Modern dark-themed window
   - Navigation bar at top (Servers, Explorer, Settings)
   - Server list view with:
     - Recent servers list (empty initially)
     - Manual URL entry
     - Discover servers button
   - Status bar at bottom showing connection status

## ?? UI Features Working Now

### Server Management
- ? Add server manually by URL
- ? View recent servers
- ? Delete servers
- ? Double-click to connect
- ? Status indicator (red/green)

### Navigation
- ? Switch between views
- ? Disabled buttons when not connected
- ? Status messages

### Storage
- ? Servers saved to: `%LocalAppData%\UAInspector\Data\servers.xml`
- ? Settings saved to: `%LocalAppData%\UAInspector\Data\settings.xml`
- ? Automatically persists on changes

## ?? What to Do Next

### Option 1: Test the UI (No OPC UA yet)

You can test the UI without OPC UA by:

1. Add a manual server URL (any text will work for now)
2. Select it and click Connect
3. The status will change to "Connected" (simulated)
4. Notice how navigation buttons enable/disable

### Option 2: Add OPC UA Functionality

Follow the **IMPLEMENTATION_GUIDE.md** to:

1. Install OPC Foundation NuGet packages
2. Implement OpcClientService
3. Implement DiscoveryService
4. Update ViewModels to use real OPC connections
5. Create Explorer view for browsing nodes
6. Add login dialog
7. Test with sample OPC UA server

### Option 3: Customize the UI

Modify `Resources/Styles.xaml` to:
- Change colors
- Adjust fonts
- Add animations
- Customize controls

## ?? Key Files to Know

| File | Purpose |
|------|---------|
| `MainWindow.xaml` | Main application window layout |
| `MainViewModel.cs` | Main window logic and navigation |
| `ServerListView.xaml` | Server list UI |
| `ServerListViewModel.cs` | Server list logic |
| `Resources/Styles.xaml` | All UI styles and colors |
| `StorageService.cs` | Data persistence |

## ?? Color Scheme

The application uses these colors (defined in `Styles.xaml`):

- **Primary**: `#0078D4` (Microsoft Blue) - Buttons, accents
- **Background**: `#1E1E1E` - Window background
- **Surface**: `#252526` - Cards, panels
- **Text**: `#FFFFFF` (white) and `#CCCCCC` (gray)
- **Success**: `#16C60C` (green) - Connected status
- **Error**: `#E81123` (red) - Disconnected status
- **Accent**: `#0078D4` (blue) - Highlights

## ?? Customization Tips

### Change the Theme

Edit `Resources/Styles.xaml`:

```xaml
<!-- Make it light theme -->
<Color x:Key="BackgroundColor">#FFFFFF</Color>
<Color x:Key="SurfaceColor">#F5F5F5</Color>
<Color x:Key="TextPrimaryColor">#000000</Color>

<!-- Change primary color to green -->
<Color x:Key="PrimaryColor">#16C60C</Color>
```

### Add Your Logo

In `MainWindow.xaml`, replace the emoji:

```xaml
<!-- Change this -->
<TextBlock Text="?" FontSize="24" .../>

<!-- To an image -->
<Image Source="Assets/logo.png" Width="32" Height="32" .../>
```

### Modify Window Title

In `MainWindow.xaml`:

```xaml
Title="Your Company - OPC UA Inspector"
```

## ?? Data Storage Location

Application data is stored in:
```
C:\Users\[YourName]\AppData\Local\UAInspector\Data\
```

Files:
- `servers.xml` - Server history
- `settings.xml` - Application settings
- `favorites.xml` - Favorite nodes (when implemented)

## ?? Troubleshooting

### Build Errors

If you see build errors:
1. Clean the solution: Build ? Clean Solution
2. Rebuild: Build ? Rebuild Solution
3. Check that all files are included in the project

### UI Not Showing Styles

If the UI looks plain:
1. Make sure `Resources/Styles.xaml` has Build Action = "Page"
2. Verify it's referenced in `App.xaml`
3. Rebuild the solution

### Can't Find Files

Make sure you're looking in the right directory:
```
C:\Users\quima\source\repos\UAInspector\UAInspector\
```

## ?? Learning Resources

### WPF & MVVM
- [Microsoft WPF Documentation](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
- [MVVM Pattern](https://docs.microsoft.com/en-us/archive/msdn-magazine/2009/february/patterns-wpf-apps-with-the-model-view-viewmodel-design-pattern)

### OPC UA
- [OPC Foundation](https://opcfoundation.org/)
- [OPC UA .NET Standard](https://github.com/OPCFoundation/UA-.NETStandard)

### XAML Styling
- [WPF Styles and Templates](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/controls/styles-templates-overview)
- [Material Design in XAML](http://materialdesigninxaml.net/)

## ?? Next Milestone

Your next goal should be to get a sample OPC UA server running and connect to it!

1. Download OPC Foundation Sample Server
2. Run it on `opc.tcp://localhost:62541`
3. Implement OpcClientService (see IMPLEMENTATION_GUIDE.md)
4. Test real connections!

---

**Current Status**: ? UI Framework Complete | ?? OPC UA Implementation Pending

**Build Status**: ? Building Successfully

Enjoy building your OPC UA client! ??
