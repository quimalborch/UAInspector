# ?? Settings View Implementada - Start with Windows

## ? Vista de Configuración Completa

### ?? Características Implementadas

#### 1. **SettingsViewModel**
- Carga y guarda configuración en `AppSettings.json`
- Gestiona registro de Windows para startup
- Detección automática del estado actual
- Save/Reset commands

#### 2. **SettingsView (UI Moderna)**
- Diseño card-based tipo SCADA
- Toggle switch animado para Start with Windows
- Secciones colapsables para futuras opciones
- Footer informativo

#### 3. **StartupHelper**
- Gestión del registro de Windows
- Path verification y actualización
- Error handling robusto

---

## ?? Diseño de Settings View

```
????????????????????????????????????????????????
? Application Settings          [?? Save] [??]  ?
? Configure UAInspector preferences      ?
????????????????????????????????????????????????
?        ?
? ???????????????????????????????????????????? ?
? ? ?? General    ? ?
? ?         ? ?
? ? ??????????????????????????????????????   ? ?
? ? ? ??  Start with Windows  [  OFF  ] ?   ? ?
? ? ?     Auto-launch when Windows starts?   ? ?
? ? ??????????????????????????????????????   ? ?
? ???????????????????????????????????????????? ?
?     ?
? ???????????????????????????????????????????? ?
? ? ?? Connection      [COMING SOON]   ? ?
? ? Connection timeout, retry settings  ? ?
? ???????????????????????????????????????????? ?
?     ?
? ???????????????????????????????????????????? ?
? ? ?? Appearance      [COMING SOON]       ? ?
? ? Theme selection, font size, UI ? ?
? ???????????????????????????????????????????? ?
?    ?
? ???????????????????????????????????????????? ?
? ? ?? About Settings        ? ?
? ? Config file: %APPDATA%\UAInspector\... ? ?
? ???????????????????????????????????????????? ?
????????????????????????????????????????????????
```

---

## ?? Start with Windows - Funcionamiento

### Cómo Funciona

#### 1. **Toggle Switch**
```xaml
[  ???  ] OFF  ?  [???  ] ON
   ? Click      ?
  Deshabilitado    Habilitado
```

**Animación suave**: 0.2 segundos de transición

#### 2. **Registro de Windows**
```
Registry Path:
HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run

Key Name: UAInspector
Value: "C:\Path\To\UAInspector.exe"
```

### Flujo Completo

```
Usuario: *click en toggle*
  ?
StartWithWindows property cambia
  ?
StartupHelper.SetStartup(true) ejecutado
  ?
Registro de Windows actualizado
  ?
Settings.StartWithWindows = true
  ?
AppSettings.json guardado
  ?
? Configuración aplicada
```

---

## ?? Arquitectura Técnica

### 1. AppSettings Model

```csharp
public class AppSettings
{
    public int ConnectionTimeout { get; set; } = 30000;
    public int SubscriptionPublishingInterval { get; set; } = 1000;
    public bool AutoReconnect { get; set; } = true;
    
    // ? Nueva propiedad
    public bool StartWithWindows { get; set; } = false;
    
    public DateTime LastUpdated { get; set; }
}
```

### 2. StartupHelper (Registro Windows)

#### Métodos Principales

**IsStartupEnabled()**
```csharp
// Verifica si existe entrada en registro
using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath))
{
    var value = key.GetValue("UAInspector") as string;
  return !string.IsNullOrEmpty(value);
}
```

**SetStartup(bool enable)**
```csharp
if (enable)
{
    // Añadir a startup
    string exePath = GetExecutablePath();
    key.SetValue("UAInspector", $"\"{exePath}\"");
}
else
{
// Remover de startup
    key.DeleteValue("UAInspector");
}
```

**VerifyStartupPath()**
```csharp
// Verifica que el path en registro coincida con ejecutable actual
var registryPath = key.GetValue("UAInspector");
var currentPath = GetExecutablePath();
return registryPath == currentPath;
```

### 3. SettingsViewModel

#### Propiedades

```csharp
public AppSettings Settings { get; set; }
public bool StartWithWindows { get; set; }
public bool IsDirty { get; set; }  // Cambios sin guardar
```

#### Comandos

```csharp
SaveCommand    // Guarda settings a JSON
ResetCommand   // Restaura defaults
ExportCommand  // Export settings (futuro)
```

#### Métodos Clave

**LoadSettings()**
```csharp
// 1. Cargar desde AppSettings.json
Settings = _storageService.LoadSettings();

// 2. Verificar registro de Windows
_startWithWindows = StartupHelper.IsStartupEnabled();

// 3. Sincronizar ambos
if (Settings.StartWithWindows != _startWithWindows)
{
    Settings.StartWithWindows = _startWithWindows;
    _storageService.SaveSettings(Settings);
}
```

**ApplyStartWithWindows(bool enable)**
```csharp
try
{
    bool success = StartupHelper.SetStartup(enable);
    
    if (success)
    {
        Settings.StartWithWindows = enable;
        // Auto-save to JSON
    }
  else
 {
        // Revert toggle
        _startWithWindows = !enable;
        OnPropertyChanged(nameof(StartWithWindows));
    }
}
catch (Exception ex)
{
    // Show error message
    // Revert toggle
}
```

---

## ?? UI Components

### Toggle Switch Animado

#### Estructura

```xaml
<Border Width="50" Height="26" CornerRadius="13">  ? Track
    <Ellipse Width="20" Height="20"/>  ? Thumb
     <TranslateTransform X="0"/>     ? Animation
    </Ellipse>
</Border>
```

#### Estados

**OFF (Unchecked)**
```
Track: Background #3E3E42 (gray)
Thumb: X=0 (left)
```

**ON (Checked)**
```
Track: Background #0078D4 (blue accent)
Thumb: X=24 (right)
Animation: 0.2s smooth transition
```

### Setting Card

#### Estructura

```xaml
???????????????????????????????????????
? [??]  Title              [Toggle]   ?
?       Description text              ?
???????????????????????????????????????
```

**Layout**:
- Icon: 40x40px con background azul
- Title: 15px SemiBold
- Description: 12px Secondary color
- Toggle: 50x26px a la derecha

### Future Settings Sections

#### Connection Settings (Grayed Out)
```
?? Connection     [COMING SOON]
Connection timeout, retry settings, and more
```

#### Appearance Settings (Grayed Out)
```
?? Appearance     [COMING SOON]
Theme selection, font size, and UI customization
```

**Opacity: 0.5** para indicar que están deshabilitadas

---

## ?? Persistencia de Datos

### AppSettings.json

#### Ubicación
```
%APPDATA%\UAInspector\AppSettings.json
```

#### Estructura
```json
{
  "ConnectionTimeout": 30000,
  "SubscriptionPublishingInterval": 1000,
  "AutoReconnect": true,
  "StartWithWindows": true,    ? Nueva configuración
  "Theme": "Dark",
  "Language": "en-US",
  "LastUpdated": "2024-12-10T14:30:00"
}
```

### Registro de Windows

#### Key Path
```
HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run
```

#### Entry
```
Name: UAInspector
Type: REG_SZ (String)
Data: "C:\Users\User\AppData\Local\UAInspector\UAInspector.exe"
```

---

## ?? Sincronización

### Al Cargar Settings

```
LoadSettings()
  ?
1. Leer AppSettings.json
  ?
2. Verificar registro Windows
  ?
3. ¿Coinciden?
   ?? Sí ? OK
   ?? No ? Sincronizar
       ?
  Actualizar JSON con estado real del registro
```

### Al Cambiar Toggle

```
Toggle Click
  ?
Property Changed
  ?
ApplyStartWithWindows()
  ?
1. Actualizar registro
  ?
2. Si éxito:
   - Actualizar Settings.StartWithWindows
   - IsDirty = true
   - Permitir Save
  ?
3. Si error:
   - Revertir toggle
   - Mostrar mensaje error
```

### Al Guardar (Save Button)

```
Save Command
  ?
Settings.LastUpdated = Now
  ?
StorageService.SaveSettings()
  ?
Write to AppSettings.json
  ?
MainViewModel.Settings actualizado
  ?
IsDirty = false
  ?
? "Settings saved successfully!"
```

---

## ?? Testing

### Test 1: Habilitar Start with Windows
1. Abrir Settings
2. Click en toggle (OFF ? ON)
3. **Verificar**: Toggle se mueve animado a derecha
4. **Verificar**: Track cambia a azul
5. Click "?? Save"
6. **Verificar**: Mensaje "Settings saved"
7. Abrir Regedit
8. **Verificar**: Entrada existe en Run key

### Test 2: Deshabilitar Start with Windows
1. En Settings, toggle ON
2. Click toggle (ON ? OFF)
3. **Verificar**: Animación smooth a izquierda
4. **Verificar**: Track gris
5. Save
6. **Verificar**: Entrada removida del registro

### Test 3: Verificar Persistencia
1. Habilitar Start with Windows
2. Save
3. Cerrar app
4. Abrir app de nuevo
5. Ir a Settings
6. **Verificar**: Toggle sigue en ON

### Test 4: Path Change Detection
1. Mover UAInspector.exe a otro folder
2. Ejecutar desde nuevo folder
3. Ir a Settings
4. **Verificar**: Toggle refleja estado correcto
5. Si estaba ON, path se actualiza automáticamente

### Test 5: Reset to Defaults
1. Cambiar settings
2. Click "?? Reset"
3. **Verificar**: Confirmación "Are you sure?"
4. Click "Yes"
5. **Verificar**: StartWithWindows = false
6. **Verificar**: Registro limpiado

---

## ?? Manejo de Errores

### Permisos Insuficientes

```csharp
catch (UnauthorizedAccessException)
{
    MessageBox.Show(
        "Failed to update Windows startup settings.\n\n" +
        "Make sure you have the necessary permissions.",
        "Startup Error",
        MessageBoxButton.OK,
  MessageBoxImage.Warning
    );
    
    // Revert toggle
}
```

### Registro No Disponible

```csharp
if (key == null)
{
Debug.WriteLine("Unable to open registry key");
 return false;
}
```

### Path Inválido

```csharp
try
{
    string exePath = Process.GetCurrentProcess().MainModule.FileName;
}
catch (Exception ex)
{
    Debug.WriteLine($"Error getting executable path: {ex.Message}");
    return false;
}
```

---

## ?? Future Enhancements

### Próximas Configuraciones

#### Connection Settings
- [ ] Connection Timeout (slider)
- [ ] Retry attempts (numeric)
- [ ] Auto-reconnect delay
- [ ] Certificate validation

#### Appearance Settings
- [ ] Theme selection (Dark/Light)
- [ ] Accent color picker
- [ ] Font size slider
- [ ] Language selection

#### Advanced Settings
- [ ] Logging level
- [ ] Data cache size
- [ ] Update check frequency
- [ ] Backup/Restore settings

---

## ? Estado Actual

**Build**: ? Exitoso
**UI**: ? Moderna y profesional
**Funcionalidad**: ? 100% operativa
**Persistencia**: ? JSON + Registry

### Archivos Creados
- ? `SettingsViewModel.cs` - Lógica completa
- ? `SettingsView.xaml` - UI moderna
- ? `SettingsView.xaml.cs` - Code-behind
- ? `StartupHelper.cs` - Registry management
- ? `AppSettings.cs` - Actualizado con StartWithWindows

### Archivos Modificados
- ? `MainViewModel.cs` - ShowSettings implementado
- ? `MainWindow.xaml` - SettingsView registrada

---

## ?? Resultado Final

**Una vista de configuración completa y funcional con:**

- ?? **Start with Windows** toggle animado
- ?? **Persistencia** en JSON y Registro
- ?? **UI moderna** tipo SCADA
- ?? **Error handling** robusto
- ?? **Sincronización** automática
- ?? **Expandible** para futuras opciones

**¡Lista para usar!** Ejecuta la app y ve a **? Settings** en el navbar. ??
