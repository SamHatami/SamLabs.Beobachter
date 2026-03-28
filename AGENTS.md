# AGENTS.md

## Purpose

Beobachter is a desktop log viewer and monitoring tool built with Avalonia UI and MVVM.

Your job is to help build a modern, maintainable rewrite of an older log console-style application.

Changes should be:

- correct
- small and reviewable
- architecturally consistent
- easy to test
- easy to extend without hidden coupling

Follow the nearest good existing pattern in the repo before introducing a new one.

---

## Project overview

Beobachter is a modern desktop log viewer focused on receiving, parsing, filtering, and inspecting log streams.

This project is inspired by older tools such as Log2Console, but it is **not** a direct port. The goal is to take the useful core ideas and rebuild them using modern .NET patterns and a cleaner architecture.

The app is built with:

- modern .NET
- Avalonia UI
- CommunityToolkit.Mvvm
- dependency injection via `Microsoft.Extensions.DependencyInjection`

### What we are working on

We are building a cross-platform desktop application for viewing and monitoring logs from different sources.

The application should support:

- receiving logs from multiple inputs such as files, TCP, and UDP
- parsing and normalizing incoming log data
- filtering, searching, and inspecting logs efficiently
- keeping UI concerns separate from ingestion and domain logic
- evolving cleanly over time without turning the UI layer into the application core

### Main goal

The goal is to create a **fast, modern, testable log viewer** with clear boundaries between:

- domain logic
- infrastructure and receivers
- UI and presentation

Prefer simple, explicit architecture over legacy desktop patterns and over-engineering.

---

## Solution structure

| Project | Role | Key dependency direction |
|---|---|---|
| `SamLabs.Beobachter.Core` | Domain/core: log models, filtering, query rules, receiver contracts, parsing contracts, session abstractions | Standalone — no UI or infrastructure deps |
| `SamLabs.Beobachter.Infrastructure` | Concrete implementations: file tailing, TCP/UDP receivers, persistence, settings storage, parser implementations | References `Core` |
| `SamLabs.Beobachter.App` | Avalonia UI: views, viewmodels, app services, composition root, theming | References `Core`, `Infrastructure` |
| `SamLabs.Beobachter.Tests` | Unit/integration tests against `Core` and selected `Infrastructure` behavior | Test-only |

**Dependency rule:** `App -> Infrastructure -> Core`, with `App` also allowed to reference `Core` directly.

### Dependency rules

- `Core` must never reference `Infrastructure` or `App`
- `Infrastructure` must never reference `App`
- `App` is the only project that should contain Avalonia types
- concrete file/network/platform code belongs in `Infrastructure`, not `Core`

### Optional future project

If orchestration becomes too large, introduce:

| Project | Role |
|---|---|
| `SamLabs.Beobachter.Application` | Use-case/application services such as session orchestration, receiver lifecycle, and workspace coordination |

Only add this if the logic between UI and infrastructure starts becoming crowded.

---

## Build & test

```powershell
dotnet build SamLabs.Beobachter.sln
dotnet test
dotnet publish SamLabs.Beobachter.App/SamLabs.Beobachter.App.csproj -c Release
```

If self-contained publishing is needed, specify the RID explicitly.

Example:

```powershell
dotnet publish SamLabs.Beobachter.App/SamLabs.Beobachter.App.csproj -c Release -r win-x64 --self-contained true
```

---

## Architectural patterns

### MVVM (App)

- Use **CommunityToolkit.Mvvm** source generators
- ViewModels should generally be `partial` classes inheriting from a shared `ViewModelBase`
- Prefer `[ObservableProperty]` and `[RelayCommand]` over handwritten notification boilerplate
- Keep ViewModels UI-facing and orchestration-focused
- Use compiled bindings where practical and set explicit `x:DataType` in AXAML
- Keep naming aligned: `FooViewModel` ↔ `FooView`

### DI and composition

- Centralize registration in one place:
  - `Program.cs`
  - `App.axaml.cs`
  - or a dedicated `CompositionRoot` / `ServiceCollectionExtensions`
- Prefer constructor injection
- Avoid service locator patterns inside ViewModels

Recommended lifetimes:

- **Singleton**: settings manager, receiver coordinator, session manager, log store
- **Transient**: lightweight helpers and factories
- **Scoped**: usually unnecessary in a desktop app unless a real scoped concept is introduced

---

## Core/domain rules

`SamLabs.Beobachter.Core` should contain only pure application concepts.

Examples:

- `LogEntry`
- `LogLevel`
- filter/query models
- search/highlight rules
- receiver abstractions such as `ILogReceiver`
- parser abstractions such as `ILogParser`
- session/workspace contracts
- settings abstractions
- pure services with no Avalonia, file system, sockets, or OS dependencies

Do **not** put concrete file watchers, TCP sockets, Avalonia types, or persistence details in `Core`.

---

## Infrastructure rules

`SamLabs.Beobachter.Infrastructure` contains all external interaction.

Examples:

- file tailing
- TCP/UDP listeners
- parser implementations
- JSON serialization
- persisted settings
- import/export logic
- OS/platform-specific services

Keep infrastructure behind interfaces defined in `Core` where appropriate.

---

## UI/App rules

`SamLabs.Beobachter.App` is responsible for:

- Views
- ViewModels
- converters
- dialogs behind UI-facing abstractions
- clipboard abstractions
- notifications
- theming and styles
- composition and startup
- presentation of logs, filters, receiver state, and detail panels

The UI should consume services rather than implementing receiver logic directly.

---

## Suggested domain data flow

```text
Receiver / FileTailer / TcpListener / UdpListener
    -> raw payload
    -> parser / normalizer
    -> LogEntry
    -> log store / session buffer
    -> filtering / search / grouping
    -> ViewModels
    -> Avalonia views
```

### Performance guidance

- Do not update the UI per incoming log line under heavy load
- Batch incoming log entries before pushing updates into observable UI collections
- Prefer an explicit producer/consumer pipeline such as channels or a queue
- Marshal only the minimum required work onto the UI thread
- Filtering and search should be designed with high-volume scenarios in mind

---

## Recommended folder conventions

### `SamLabs.Beobachter.Core`

- `Models`
- `Enums`
- `Filters`
- `Queries`
- `Interfaces`
- `Services`
- `Options`

### `SamLabs.Beobachter.Infrastructure`

- `Receivers`
- `Parsing`
- `Persistence`
- `Configuration`
- `Platform`

### `SamLabs.Beobachter.App`

- `Features`
- `Views`
- `ViewModels`
- `Controls`
- `Converters`
- `Services`
- `Themes`
- `Resources`

### `SamLabs.Beobachter.Tests`

- `Core`
- `Infrastructure`
- `Helpers`
- `TestData`

---

## Receiver design conventions

When adding a new receiver:

- define the abstraction in `Core`
- implement the concrete receiver in `Infrastructure`
- keep lifecycle explicit: start, stop, dispose
- isolate transport-specific concerns from parsing concerns
- make cancellation handling explicit
- avoid hidden background threads without ownership
- surface failures in a controlled way so the UI can report receiver state clearly

A receiver should be responsible for acquiring data, not for UI presentation.

---

## Filtering and search conventions

Filtering is a core feature of the product and should be treated as domain logic, not UI-only behavior.

Keep filter logic in `Core` where possible:

- text search
- log level filtering
- source filtering
- timestamp filtering
- category/tag filtering
- highlight rules
- match expressions

The UI may provide filter editors/builders, but the actual evaluation logic should stay outside the view layer.

---

## Settings and persistence

- Persist app settings in a user-local location
- Keep the persistence format simple and versionable, preferably JSON
- Separate:
  - app settings
  - receiver definitions
  - workspace/session settings
  - UI layout preferences
- New settings should be added through typed settings models, not scattered string keys

Suggested root location:

```text
%LOCALAPPDATA%/SamLabs.Beobachter/
```

Adjust exact filenames once the settings model is finalized.

---

## UI theming

- Centralize colors, brushes, spacing, and reusable styles in `App/Themes/`
- Prefer reusable resource dictionaries over hardcoded values in views
- Keep styling consistent across log list, receiver panels, filter controls, search surfaces, and detail panes
- Avoid embedding major styling decisions directly into individual views unless they are truly local

---

## Testing guidance

Test `Core` aggressively.

Priority areas:

- parser behavior
- filter matching
- query behavior
- log normalization
- session/log-store behavior
- malformed or partial log input
- receiver lifecycle state transitions

Test `Infrastructure` where behavior is deterministic:

- parser implementations
- file tailing behavior
- settings serialization
- receiver start/stop/dispose behavior

Avoid making UI tests the primary safety net for core logic.

---

## Conventions to follow

- **Detailed MVVM and Avalonia rules** are in `.github/instructions/MVVM-Avalonia-general-instruction.md`
- **Detailed C# style rules** are in `.github/instructions/csharp-style-instructions.md`
- Keep `Core` dependency-free from UI and infrastructure concerns
- Keep concrete external integrations in `Infrastructure`
- Keep Avalonia-specific behavior in `App`
- Prefer constructor injection
- Prefer CommunityToolkit.Mvvm source generators over handwritten boilerplate
- Do not leak Avalonia types into `Core`
- Do not put receiver, parser, file, or network implementations in `Core`
- New features should usually begin with models/contracts in `Core`, then concrete implementations in `Infrastructure`, then UI wiring in `App`

---

## Practical project intent

Beobachter is not a generic framework. It is a focused desktop product.

When making design decisions, prefer:

- clarity over cleverness
- maintainability over over-abstraction
- fast iteration over premature architecture
- modern .NET patterns over legacy desktop habits
- explicit boundaries over “shared utility” sprawl
