# UAInspector - Modern OPC UA Client

A modern, elegant, and fluent OPC UA client application built with WPF and MVVM architecture.

## ?? Features

- **Modern Fluent Design UI** - Dark theme with smooth animations
- **Server Management** - Connect to OPC UA servers with support for:
  - Manual URL entry
  - Recent servers history
  - Network discovery (ready for implementation)
- **Multiple Authentication Methods**:
  - Anonymous
  - Username/Password
  - Certificate-based
- **MVVM Architecture** - Clean separation of concerns
- **Local Storage** - Persist server history and settings
- **Future Ready** - Prepared for OPC UA browsing and subscriptions

## ?? Project Structure

```
UAInspector/
??? Core/
?   ??? Models/
?   ?   ??? OpcServerInfo.cs # Server information model
?   ?   ??? OpcNodeInfo.cs        # OPC UA node model
?   ?   ??? AppSettings.cs    # Application settings
?   ??? Services/
? ??? StorageService.cs     # Local data persistence
??? ViewModels/
?   ??? ViewModelBase.cs    # Base ViewModel with INotifyPropertyChanged
?   ??? RelayCommand.cs           # ICommand implementation
?   ??? MainViewModel.cs          # Main window ViewModel
?   ??? ServerListViewModel.cs # Server list ViewModel
??? Views/
?   ??? ServerListView.xaml       # Server list UI
?   ??? ServerListView.xaml.cs
??? Resources/
?   ??? Styles.xaml     # Modern Fluent Design styles
??? MainWindow.xaml       # Main application window
??? MainWindow.xaml.cs
??? App.xaml
??? App.xaml.cs
```

## ?? Getting Started

### Prerequisites

- .NET Framework 4.8.1
- Visual Studio 2019 or later

### Installation

1. Clone the repository
2. Open `UAInspector.sln` in Visual Studio
3. Restore NuGet packages (when OPC UA packages are added)
4. Build and run

## ?? Next Steps - Adding OPC UA Functionality

To complete the OPC UA implementation, you need to:

### 1. Install OPC Foundation NuGet Packages

Since this is a .NET Framework project with old-style .csproj, manually add to your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="OPCFoundation.NetStandard.Opc.Ua" Version="1.5.374.86" />
  <PackageReference Include="OPCFoundation.NetStandard.Opc.Ua.Client" Version="1.5.374.86" />
</ItemGroup>
```

Or use NuGet Package Manager in Visual Studio:
- Right-click project ? Manage NuGet Packages
- Search for "OPCFoundation.NetStandard.Opc.Ua"
- Install the Client package

### 2. Implement Core OPC UA Services

Create these service files in `Core/Services/`:

#### `OpcClientService.cs`
```csharp
- Initialize ApplicationConfiguration
- Connect to OPC UA server
- Handle sessions
- Browse nodes
- Read/Write values
- Subscribe to data changes
```

#### `DiscoveryService.cs`
```csharp
- Discover servers on local network
- Find endpoints
- Validate server certificates
```

#### `CertificateManager.cs`
```csharp
- Manage application certificates
- Store/retrieve user certificates
- Handle certificate trust
```

### 3. Create Additional ViewModels

#### `LoginViewModel.cs`
- Handle authentication dialogs
- Support Anonymous, Username/Password, Certificate login
- Remember credentials option

#### `ExplorerViewModel.cs`
- Browse OPC UA address space
- Display node hierarchy (TreeView)
- Show selected node details
- Read/Write values
- Subscribe to node changes

#### `SettingsViewModel.cs`
- Application preferences
- Connection timeouts
- Subscription settings
- Certificate management UI

### 4. Create Additional Views

#### `LoginView.xaml`
- Authentication dialog
- Input fields based on login type
- Certificate selector

#### `ExplorerView.xaml`
- Split panel layout:
  - Left: TreeView for node hierarchy
  - Right: Details panel with properties and actions

#### `SettingsView.xaml`
- Tabs for different setting categories
- Certificate management interface

## ?? UI Design Guidelines

The application uses a modern dark theme with:
- **Primary Color**: #0078D4 (Microsoft Blue)
- **Background**: #1E1E1E (Dark Gray)
- **Surface**: #252526 (Lighter Dark Gray)
- **Text**: White (#FFFFFF) and light gray (#CCCCCC)

All UI components follow Fluent Design principles with:
- Rounded corners (4-8px)
- Smooth hover states
- Clean typography
- Ample whitespace

## ?? Technologies

- **C# 7.3** with .NET Framework 4.8.1
- **WPF** for UI
- **MVVM** pattern
- **OPC Foundation .NET Standard** (ready to add)
- **System.Text.Json** for local storage

## ?? Data Storage

Application data is stored in:
```
%LocalAppData%\UAInspector\Data\
??? servers.json      # Server history
??? settings.json     # Application settings
??? favorites.json    # Favorite nodes
```

## ??? Development Notes

- The project is currently scaffolded with the UI framework and basic navigation
- OPC UA functionality needs to be implemented (see "Next Steps" above)
- Storage service is implemented using JSON files (can be upgraded to LiteDB)
- All styles are centralized in `Resources/Styles.xaml`

## ?? License

This project is open source and available under the MIT License.

## ?? Contributing

Contributions are welcome! Please feel free to submit pull requests.

---

**Status**: ?? UI Framework Complete - OPC UA Implementation Pending
