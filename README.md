# CthulhuSheets

A Call of Cthulhu 7e investigator sheet app (Blazor WebAssembly + MudBlazor).

## Run the app

```sh
dotnet run --project CthulhuSheets
```

## Run tests

```sh
dotnet test
```

Unit tests live in `CthulhuSheets.Tests/` and cover the deterministic rules logic —
derived-stat formulas (damage bonus/build, HP/MP/SAN/MOV), dice mechanics, skill
experience-check rules, occupation skill-point math — plus a data cross-validation
suite that fails the build if the static game data (`Occupations.cs`, `DefaultSkills.cs`)
drifts out of sync.
