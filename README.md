# TP Mechanical Revit Tools

`TPMechanical.RevitTools.sln` is the company Revit 2026 add-in solution.

- `TPMechanical.Addin` owns Revit startup and the TP Mechanical ribbon.
- `TPMechanical.HangerPlacement` owns the working hanger command, WPF window,
  rule/session models, fabrication discovery, placement logic, and button icon.
- `TPMechanical.QAQC` remains available but is on hold.

All projects target `net8.0-windows` and x64. Revit references default to
`C:\Program Files\Autodesk\Revit 2026`; a developer can override that path with the
`RevitApiDirectory` MSBuild property.

The Addin project references the feature projects so its output folder contains the
three assemblies Revit needs. The installed Revit manifest points only to
`TPMechanical.Addin.dll` and `TPMechanical.Addin.TPMechanicalApp`.

`TPMechanical.Shared` is intentionally deferred until two feature projects actually
need the same code. That avoids creating an empty dependency and keeps ownership clear.
