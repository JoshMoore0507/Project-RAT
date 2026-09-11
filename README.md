# Project RAT

**Revit App Tools for TP Mechanical**

Project RAT is TP Mechanical's private Revit 2026 add-in. It provides one organized ribbon for drawing QA/QC, fabrication hanger placement, layout-point workflows, and future field and coordination tools.

## Current tools

- **Drawing QA/QC** — foundation for automated drawing and model checks.
- **Hanger Placement** — rule-based hanger selection, planning, validation, and placement for fabrication parts.

## Planned tools

- Trimble and layout-point creation
- Connector and system validation
- Collision-aware hanger adjustment
- Structural attachment workflows
- Controlled release packaging for company deployment

See [the roadmap](docs/ROADMAP.md) for the working feature list.

## Development requirements

- Visual Studio 2022
- .NET 8 SDK
- Autodesk Revit 2026
- Access to `RevitAPI.dll` and `RevitAPIUI.dll`

Open `TPMechanical.QAQC.slnx` in Visual Studio. Each developer should build and test against their own local Revit installation; generated `bin`, `obj`, and `.vs` files are intentionally excluded from Git.

## Collaboration

`main` is the tested version. Create a short-lived branch for each feature or repair, push it, and merge it through a pull request after review and Revit testing.

Examples:

- `feature/hanger-spacing`
- `feature/layout-points`
- `fix/ribbon-icon`

Read [the collaboration guide](COLLABORATION.md) before making the first change.

