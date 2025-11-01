# .NET 8.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 8.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 8.0 upgrade.
3. Upgrade UAInspector\UAInspector.csproj to .NET 8.0

## Settings

This section contains settings and data used by execution steps.

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name        | Current Version | New Version | Description               |
|:-----------------------------------------------------|:---------------:|:-----------:|:-----------------------------------------------------------|
| Microsoft.Bcl.AsyncInterfaces             | 9.0.9         | 8.0.0       | Recommended for .NET 8.0        |
| Microsoft.Extensions.DependencyInjection.Abstractions| 9.0.9      | 8.0.2       | Recommended for .NET 8.0          |
| Microsoft.Extensions.Logging.Abstractions      | 9.0.9           | 8.0.3       | Recommended for .NET 8.0               |
| Newtonsoft.Json     | 13.0.3          | 13.0.4      | Recommended for .NET 8.0  |
| System.Buffers        | 4.5.1           |      | Package functionality included with .NET 8.0 framework     |
| System.Diagnostics.DiagnosticSource             | 9.0.9 | 8.0.1       | Recommended for .NET 8.0       |
| System.Formats.Asn1         | 9.0.9         | 8.0.2       | Recommended for .NET 8.0 |
| System.Memory         | 4.5.5  |      | Package functionality included with .NET 8.0 framework     |
| System.Numerics.Vectors         | 4.5.0           |             | Package functionality included with .NET 8.0 framework     |
| System.Runtime.CompilerServices.Unsafe    | 6.0.0        | 6.1.2| Recommended for .NET 8.0   |
| System.Threading.Tasks.Extensions                | 4.5.4         |             | Package functionality included with .NET 8.0 framework     |
| System.ValueTuple             | 4.5.0        |        | Package functionality included with .NET 8.0 framework     |

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### UAInspector\UAInspector.csproj modifications

Project structure changes:
  - Project file needs to be converted to SDK-style format

Project properties changes:
  - Target framework should be changed from `net481` to `net8.0-windows`

NuGet packages changes:
- Microsoft.Bcl.AsyncInterfaces should be updated from `9.0.9` to `8.0.0` (*recommended for .NET 8.0*)
  - Microsoft.Extensions.DependencyInjection.Abstractions should be updated from `9.0.9` to `8.0.2` (*recommended for .NET 8.0*)
  - Microsoft.Extensions.Logging.Abstractions should be updated from `9.0.9` to `8.0.3` (*recommended for .NET 8.0*)
  - Newtonsoft.Json should be updated from `13.0.3` to `13.0.4` (*recommended for .NET 8.0*)
  - System.Diagnostics.DiagnosticSource should be updated from `9.0.9` to `8.0.1` (*recommended for .NET 8.0*)
  - System.Formats.Asn1 should be updated from `9.0.9` to `8.0.2` (*recommended for .NET 8.0*)
  - System.Runtime.CompilerServices.Unsafe should be updated from `6.0.0` to `6.1.2` (*recommended for .NET 8.0*)
  - System.Buffers should be removed (*package functionality included with .NET 8.0 framework*)
  - System.Memory should be removed (*package functionality included with .NET 8.0 framework*)
  - System.Numerics.Vectors should be removed (*package functionality included with .NET 8.0 framework*)
- System.Threading.Tasks.Extensions should be removed (*package functionality included with .NET 8.0 framework*)
  - System.ValueTuple should be removed (*package functionality included with .NET 8.0 framework*)
