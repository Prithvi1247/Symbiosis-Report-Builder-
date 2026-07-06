# Architectural Manifest

## Design Guidelines
- **Configuration/**: Keeps `Program.cs` slim by containing extension workflows for IServiceCollection setups.
- **Interfaces/**: Houses decoupling mechanics for underlying ingestion engines and repository structures.
- **ViewModels/**: Strictly contains UI presentation structures. Domain models from `Models/` or `Data/` are not passed directly to Views.
- **Utilities/**: Cross-cutting string operations or pure-function manipulations stripped of business framework dependencies.