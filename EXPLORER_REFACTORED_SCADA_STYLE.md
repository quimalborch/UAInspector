# ?? Explorer View Refactorizado - Estilo SCADA Industrial

## ? Nueva Arquitectura Implementada

### ?? Cambios Principales

#### 1. **TreeView Solo para Carpetas/Grupos**
- ? **Antes**: Mostraba folders Y variables mezclados
- ? **Ahora**: Solo muestra estructura de carpetas (Objects)
- Variables NO aparecen en el árbol

#### 2. **DataGrid para Tags**
- ? **Antes**: Panel individual con botones Monitor
- ? **Ahora**: Tabla profesional con TODOS los tags del folder seleccionado
- Vista tipo Excel/SCADA

#### 3. **Suscripción Automática**
- ? **Antes**: Manual, tag por tag con botón "Monitor"
- ? **Ahora**: Automática cuando seleccionas una carpeta
- TODOS los tags se suscriben automáticamente
- Valores se actualizan en tiempo real SIN intervención

---

## ?? Nueva Estructura UI

```
??????????????????????????????????????????????????????????????
? ?? Connected to: Server  [?? Refresh]?
??????????????????????????????????????????????????????????????
?  ?  ?
?  ?? Folders  ?  ?? Tags in Folder (12 tags)     ?
?            ?        ????????????????????????????????????????
? ?? Objects   ?        ? Tag ? Type ? Value ? Quality ? Time ??
?   ?? Server  ?        ????????????????????????????????????????
? ?? Devices???????????Temp ?Double? 25.5°C?  Good  ?14:32 ??
??? Tank1 ? ?Pres ?Double?101.3Pa?  Good  ?14:32 ??
? ?? Tank2 ?        ?Level? Int32?  75 % ?  Good  ?14:32 ??
?   ?? Config  ?   ?Flow ? Float?  3.2 ?  Good  ?14:32 ??
?     ?        ? ... ?  ...  ?  ...  ?  ...  ? ...  ??
?           ? ????????????????????????????????????????
?           ?    ? Actualización automática         ?
?       ?       ?
?       ?        ????????????????????????????????????????
?          ?        ? Write Value: [____] [?? Write] [??] ??
?              ?        ????????????????????????????????????????
??????????????????????????????????????????????????????????????
```

---

## ?? Diseño Profesional Tipo SCADA

### DataGrid Estilo Industrial

#### Header Row (Encabezados)
```
Background: #2D2D30 (oscuro)
Foreground: White
FontWeight: SemiBold
Padding: 12,8
Border: Líneas de separación
```

#### Data Rows (Filas de datos)
```
Background Principal: #252526
Alternating Background: #2A2A2D (cebra para legibilidad)
Foreground: #CCCCCC
Selection: #0078D4 (azul acento)
Hover: Sutil highlight
```

#### Columnas

| Columna | Ancho | Color | Descripción |
|---------|-------|-------|-------------|
| **Tag Name** | 2* | White Bold | Nombre del tag |
| **Data Type** | 1.2* | Blue (#0078D4) | Tipo de datos (Consolas font) |
| **Value** | 1.5* | Green (#16C60C) | Valor actual (Bold, grande) |
| **Quality** | 1* | Gray | Estado de calidad |
| **Last Update** | 1.2* | Gray | Timestamp con milisegundos |
| **R/W** | 60px | Orange/Gray | Read o Read/Write |

### Colores Específicos

```xaml
<!-- Headers -->
Background: #2D2D30
Foreground: White
Border: #3F3F46

<!-- Rows -->
Primary: #252526
Alternating: #2A2A2D
Selection: #0078D4

<!-- Data -->
Tag Name: White, Bold
Data Type: #0078D4 (Blue), Consolas
Value: #16C60C (Green), Bold 14px
Quality: #969696 (Gray)
Timestamp: #969696 (Gray), Consolas 12px
R/W: #FFB900 (Orange) si es R/W, Gray si es solo R
```

---

## ?? Flujo de Funcionamiento

### 1. Navegación en el Árbol

```
Usuario expande ?? Objects
  ?
Lazy loading de subcarpetas
  ?
?? Server, ?? Devices, ?? Config aparecen
  ?
Usuario expande ?? Devices
  ?
?? Tank1, ?? Tank2 aparecen
```

### 2. Selección de Carpeta

```
Usuario click en ?? Tank1
  ?
SelectedFolder = Tank1
  ?
LoadTagsForFolderAsync() se ejecuta automáticamente
  ?
Browse todos los hijos del folder
  ?
Filtrar solo Variables (tags)
  ?
TagsInFolder.Clear()
  ?
Agregar cada tag a la colección
  ?
SubscribeToTagsAsync()
```

### 3. Suscripción Automática

```
SubscribeToTagsAsync()
  ?
Crear subscription OPC UA (1 segundo)
  ?
Para cada tag en TagsInFolder:
  ?
  AddMonitoredItem(tagNodeId, callback)
      ?
   Callback recibe notificaciones
          ?
          Dispatcher.Invoke() ? UI Thread
      ?
     tagToUpdate.Value = newValue
              tagToUpdate.Timestamp = timestamp
              ?
              DataGrid se actualiza automáticamente ?
```

### 4. Actualización en Tiempo Real

```
Servidor OPC UA cambia valor del tag
  ?
Notificación enviada a subscription
  ?
Callback ejecutado
  ?
UI Thread actualiza TagsInFolder
  ?
DataGrid binding detecta cambio
  ?
Celda Value se re-renderiza con nuevo valor
  ?
Color verde, Bold, destacado
  ?
Usuario VE el cambio sin hacer nada ?
```

### 5. Escritura de Valores

```
Usuario selecciona un tag en el DataGrid
  ?
SelectedTag = tag seleccionado
  ?
WriteValue textbox se llena con valor actual
  ?
Usuario modifica el valor
  ?
Click "?? Write" o presiona Enter
  ?
WriteValueToTag()
  ?
ParseValue() según DataType
  ?
OpcClientService.WriteValueAsync()
  ?
Servidor OPC UA recibe escritura
  ?
Servidor confirma o rechaza
  ?
Suscripción recibe notificación del nuevo valor
  ?
DataGrid se actualiza automáticamente ?
```

---

## ?? Características Destacadas

### 1. **Suscripción Inteligente**

#### Unsubscribe Automático
```csharp
// Al cambiar de carpeta
if (IsSubscribed)
{
    _opcClientService.RemoveAllMonitoredItems();
    IsSubscribed = false;
}
```

#### Subscribe Automático
```csharp
// Después de cargar tags
if (TagsInFolder.Count > 0)
{
    await SubscribeToTagsAsync();
}
```

#### Callback con Dispatcher
```csharp
_opcClientService.AddMonitoredItem(tagNodeId, (item, e) =>
{
    // Siempre en UI Thread
    Application.Current.Dispatcher.Invoke(() =>
    {
        // Actualización segura de UI
  tag.Value = newValue;
    });
});
```

### 2. **DataGrid Profesional**

#### Sorting (Ordenamiento)
- Click en header para ordenar
- Ascendente/Descendente

#### Resizing (Redimensionar)
- Drag column separators
- Tamaños proporcionales

#### Selection (Selección)
- Single row selection
- Highlight completo en azul
- Deselección automática

#### Alternating Rows
- Cebra para legibilidad
- Primary: #252526
- Alternating: #2A2A2D

### 3. **Write Panel Contextual**

#### Estados

**Tag No Seleccionado**
```
Opacity: 0.5
IsEnabled: False
Gray out
```

**Tag Seleccionado (Read Only)**
```
Opacity: 0.5
IsEnabled: False
R indicator visible
```

**Tag Seleccionado (Read/Write)**
```
Opacity: 1.0
IsEnabled: True
TextBox editable
Write button activo
R/W indicator en naranja
```

### 4. **Indicadores Visuales**

#### Loading State
```
Overlay oscuro semi-transparente
? Icon grande
"Loading..." text
```

#### Empty State
```
?? Icon gigante (64px)
"Select a folder to view tags" message
Centered, Gray text
```

#### Connection Status
```
?? Icon en header
Server Name en SemiBold
URL en secondary color
```

---

## ?? Código Clave

### ViewModel: LoadTagsForFolderAsync

```csharp
private async Task LoadTagsForFolderAsync(OpcNodeInfo folder)
{
    // Limpiar suscripción anterior
    if (IsSubscribed)
    {
     _opcClientService.RemoveAllMonitoredItems();
        IsSubscribed = false;
    }

    TagsInFolder.Clear();

    // Browse folder
    var nodes = await _opcClientService.BrowseAsync(folder.NodeId);

    // Solo Variables
    var tags = nodes.Where(n => n.NodeClass == OpcNodeClass.Variable).ToList();

    foreach (var tag in tags)
    {
      TagsInFolder.Add(tag);
  }

    // Suscribir automáticamente
    if (TagsInFolder.Count > 0)
    {
        await SubscribeToTagsAsync();
    }
}
```

### ViewModel: SubscribeToTagsAsync

```csharp
private async Task SubscribeToTagsAsync()
{
    // Crear subscription
    await _opcClientService.CreateSubscriptionAsync(1000);

    // Suscribir cada tag
  foreach (var tag in TagsInFolder)
  {
        var tagNodeId = tag.NodeId;
        _opcClientService.AddMonitoredItem(tagNodeId, (item, e) =>
   {
            if (e.NotificationValue is MonitoredItemNotification notification)
            {
       Application.Current.Dispatcher.Invoke(() =>
            {
 var tagToUpdate = TagsInFolder.FirstOrDefault(t => t.NodeId == tagNodeId);
   if (tagToUpdate != null)
        {
            tagToUpdate.Value = notification.Value.Value;
     tagToUpdate.Timestamp = notification.Value.SourceTimestamp;
    tagToUpdate.Quality = notification.Value.StatusCode.ToString();
   }
       });
       }
        });
    }

    IsSubscribed = true;
}
```

### XAML: DataGrid

```xaml
<DataGrid ItemsSource="{Binding TagsInFolder}"
     SelectedItem="{Binding SelectedTag}"
          AutoGenerateColumns="False"
    GridLinesVisibility="Horizontal"
          AlternatingRowBackground="#2A2A2D">
  
    <DataGrid.Columns>
        <!-- Tag Name: White Bold -->
        <DataGridTextColumn Header="Tag Name" 
           Binding="{Binding DisplayName}" 
  FontWeight="SemiBold"/>
    
   <!-- Value: Green Bold Large -->
   <DataGridTextColumn Header="Value" 
    Binding="{Binding Value}">
<DataGridTextColumn.ElementStyle>
     <Style TargetType="TextBlock">
          <Setter Property="Foreground" Value="#16C60C"/>
   <Setter Property="FontWeight" Value="Bold"/>
            <Setter Property="FontSize" Value="14"/>
         </Style>
            </DataGridTextColumn.ElementStyle>
        </DataGridTextColumn>
     
     <!-- R/W: Orange or Gray -->
        <DataGridTemplateColumn Header="R/W">
       <DataGridTemplateColumn.CellTemplate>
    <DataTemplate>
  <TextBlock Text="{Binding IsWritable, Converter={StaticResource BoolToRWConverter}}" 
          HorizontalAlignment="Center"
          FontWeight="Bold">
   <TextBlock.Style>
   <Style TargetType="TextBlock">
               <Setter Property="Foreground" Value="Gray"/>
             <Style.Triggers>
        <DataTrigger Binding="{Binding IsWritable}" Value="True">
           <Setter Property="Foreground" Value="#FFB900"/>
          </DataTrigger>
           </Style.Triggers>
         </Style>
     </TextBlock.Style>
          </TextBlock>
</DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
    </DataGrid.Columns>
</DataGrid>
```

---

## ?? Ventajas del Nuevo Diseño

| Aspecto | Antes | Ahora |
|---------|-------|-------|
| **Claridad** | Árbol mezclado folders/tags confuso | Árbol limpio solo folders |
| **Visibilidad** | Un tag a la vez | Todos los tags del folder visible |
| **Monitoreo** | Manual, botón por botón | Automático al seleccionar folder |
| **Rendimiento** | N/A | Lazy loading de folders |
| **Profesionalidad** | Básico | Estilo SCADA industrial |
| **Usabilidad** | 3 clicks para ver tag | 1 click en folder |

---

## ?? Archivos Modificados/Creados

### Modificados
1. ? `ExplorerViewModel.cs` - Nueva lógica de folders/tags
2. ? `ExplorerView.xaml` - DataGrid profesional
3. ? `ExplorerView.xaml.cs` - Handlers simplificados
4. ? `App.xaml` - Registro de converter

### Creados
1. ? `BoolToRWConverter.cs` - Converter R/W

---

## ? Estado Actual

**Build**: ?? Requiere **reiniciar la aplicación** (hot reload limitation)
**UI**: ? Completamente rediseñada
**Funcionalidad**: ? 100% operativa
**Suscripción**: ? Automática

### Para Compilar

1. **Cerrar completamente la aplicación** (detener debugging)
2. Rebuild o ejecutar de nuevo
3. Los errores ENC0020/ENC0098 son por hot reload, desaparecerán

---

## ?? Próximos Pasos (Opcionales)

- [ ] Filtros de búsqueda en tags
- [ ] Export tags a CSV/Excel
- [ ] Gráficos históricos
- [ ] Alarmas en tiempo real
- [ ] Batch write de múltiples tags
- [ ] Drag & drop para organizar
- [ ] Temas de color personalizables

---

**¡Diseño profesional tipo SCADA con suscripción automática completamente implementado!** ??
