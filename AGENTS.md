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
- readable by humans first

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
| `SamLabs.Beobachter.Core` | Domain/core: log models, filtering, query rules, receiver contracts, parsing/framing contracts, store contracts, settings contracts | Standalone — no UI or infrastructure deps |
| `SamLabs.Beobachter.Infrastructure` | Concrete implementations: file tailing, TCP/UDP receivers, framing, parser implementations, persistence, settings storage | References `Core` |
| `SamLabs.Beobachter.Application` | Avalonia UI: views, viewmodels, app services, composition root, theming, presentation orchestration | References `Core`, `Infrastructure` |
| `SamLabs.Beobachter.Tests` | Unit/integration tests against `Core` and selected `Infrastructure` and `Application` behavior | Test-only |

**Dependency rule:** `Application -> Infrastructure -> Core`, with `Application` also allowed to reference `Core` directly.

### Dependency rules

- `Core` must never reference `Infrastructure` or `Application`
- `Infrastructure` must never reference `Application`
- `Application` is the only project that should contain Avalonia types
- concrete file/network/platform code belongs in `Infrastructure`, not `Core`
- domain filtering and query logic belongs in `Core`, not in views or code-behind

### Optional future project

If orchestration becomes too large, introduce a separate project such as:

| Project | Role |
|---|---|
| `SamLabs.Beobachter.Orchestration` | Use-case/application services such as receiver lifecycle coordination, session orchestration, and workspace coordination |

Do **not** create this unless the logic between UI and infrastructure is clearly becoming crowded.

---

## Build & test

```powershell
dotnet build SamLabs.Beobachter.sln
dotnet test
dotnet publish SamLabs.Beobachter.Application/SamLabs.Beobachter.Application.csproj -c Release
```

If self-contained publishing is needed, specify the RID explicitly.

Example:

```powershell
dotnet publish SamLabs.Beobachter.Application/SamLabs.Beobachter.Application.csproj -c Release -r win-x64 --self-contained true
```

---

## Architectural guardrails

### Patterns that are encouraged

Use patterns that directly improve clarity, testability, and throughput for this product:

- **Producer / Consumer** for ingest pipelines
  - Receivers write to a session-owned `ChannelWriter<LogEntry>`
  - A single consumer reads from the `ChannelReader<LogEntry>`
  - Batch before updating UI-facing collections
- **Strategy** for parsers
  - Receivers should not hardcode parsing logic inline
  - Prefer `ILogParser` implementations composed in an ordered pipeline
- **Strategy** for framing
  - Stream/file receivers should use explicit framing abstractions
  - Prefer an interface such as `ILogPayloadFramer` with concrete implementations like XML-event framing, line-delimited framing, or passthrough framing
- **Purpose-built store boundary**
  - Use a dedicated `ILogStore` or equivalent product-specific abstraction
  - Keep it focused on append/query/snapshot responsibilities
- **Coordinator / orchestration service** where it genuinely reduces complexity
  - Example: receiver lifecycle coordination extracted from a large session class
- **Explicit runtime state models**
  - Prefer clear models such as receiver runtime state, health, last error, and last activity over implicit state hidden in UI strings

### Patterns to avoid

Do **not** add patterns that obscure the data flow:

- no generic `IRepository<T>` or generic CRUD repository abstractions for logs
- no service locator in views or viewmodels
- no second pub-sub or event-aggregator system for the main log-entry data flow
- no framework-heavy abstraction layers when a small product-specific interface is clearer
- no “manager”, “helper”, or “utils” dumping grounds when a focused type can be named precisely

### Data-flow guardrail

The main data path should stay explicit and easy to trace:

```text
Receiver transport
    -> framing
    -> parser
    -> LogEntry
    -> channel
    -> batch consumer
    -> log store
    -> filtering / query
    -> ViewModels
    -> Avalonia views
```

Never route the primary log-entry stream through UI messengers, view events, or ad-hoc static callbacks.

---

## MVVM (Application)

- Use **CommunityToolkit.Mvvm** source generators
- ViewModels should generally be `partial` classes inheriting from a shared `ViewModelBase`
- Prefer `[ObservableProperty]` and `[RelayCommand]` over handwritten notification boilerplate
- Keep ViewModels UI-facing and orchestration-focused
- Use compiled bindings where practical and set explicit `x:DataType` in AXAML
- Keep naming aligned: `FooViewModel` ↔ `FooView`
- Keep viewmodels free of Avalonia control types whenever practical
- Prefer command binding, data binding, and behaviors over code-behind event handlers
- Avoid direct view-to-viewmodel reach-through from code-behind

### Code-behind rules

- Avoid code-behind unless the behavior is truly view-only and cannot be expressed cleanly through binding, commands, behaviors, attached properties, or existing Avalonia primitives
- Allowed code-behind examples:
  - bootstrapping generated view initialization
  - narrowly scoped view-only interaction that has no domain meaning
  - temporary framework glue when there is no clean declarative alternative
- Not allowed in code-behind:
  - business logic
  - filtering/query logic
  - receiver/session orchestration
  - settings persistence
  - cross-view coordination that belongs in a ViewModel or service

### Collection and filtering guidance

- Prefer framework-provided collection-view and filtering primitives where they fit the UI surface instead of inventing ad-hoc view filtering layers
- In WPF-style surfaces, that includes `ICollectionView` and built-in filter/sort/group support
- In Avalonia, prefer the closest built-in collection-view abstraction available for the control surface before creating custom wrappers
- Keep **filter definitions and matching logic** in `Core`
- Use UI collection views for presentation concerns such as sorting, grouping, and visible projection when appropriate, but do not move domain filtering rules into the view layer

### View rules

- Keep AXAML declarative and focused on structure, binding, and reusable resources
- Do not put styling decisions directly in views unless they are truly tiny and local
- Prefer resource dictionaries, theme dictionaries, styles, control themes, and reusable templates
- Prefer converters sparingly; when logic becomes non-trivial, move it into the ViewModel or a dedicated service

---

## DI and composition

- Centralize registration in one place:
  - `Program.cs`
  - `App.axaml.cs`
  - or a dedicated composition root / service registration extensions
- Prefer constructor injection
- Avoid service locator patterns inside ViewModels

Recommended lifetimes:

- **Singleton**: settings store, receiver coordinator, ingestion session, log store, theme service
- **Transient**: lightweight helpers, factories, stateless translators
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
- framing abstractions such as `ILogPayloadFramer`
- session/workspace contracts
- settings abstractions
- store contracts
- pure services with no Avalonia, file system, sockets, or OS dependencies

Do **not** put concrete file watchers, TCP sockets, Avalonia types, or persistence details in `Core`.

---

## Infrastructure rules

`SamLabs.Beobachter.Infrastructure` contains all external interaction.

Examples:

- file tailing
- TCP/UDP listeners
- framing implementations
- parser implementations
- JSON serialization
- persisted settings
- import/export logic
- OS/platform-specific services

Keep infrastructure behind interfaces defined in `Core` where appropriate.

### Receiver design conventions

When adding or changing a receiver:

- define the abstraction in `Core`
- implement the concrete receiver in `Infrastructure`
- keep lifecycle explicit: start, stop, dispose
- isolate transport-specific concerns from framing concerns
- isolate framing concerns from parsing concerns
- make cancellation handling explicit
- avoid hidden background threads without ownership
- surface failures in a controlled way so the UI can report receiver state clearly

A receiver should be responsible for acquiring data, not for UI presentation.

---

## UI/Application rules

`SamLabs.Beobachter.Application` is responsible for:

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

### Styling rules

- Centralize colors, brushes, spacing, icons, and reusable styles in `Application/Themes/` and shared resources
- Prefer resource dictionaries over inline property styling in AXAML
- Do not embed substantial styling directly in views
- Do not hardcode repeated brushes, margins, font sizes, or control templates in feature views
- Keep control themes reusable and named clearly
- Use bindings and theme resources rather than view-local visual constants when values are reused or part of app identity

---

## Performance guidance

- Do not update the UI per incoming log line under heavy load
- Batch incoming log entries before pushing updates into observable UI collections
- Prefer an explicit producer/consumer pipeline such as channels or a queue
- Marshal only the minimum required work onto the UI thread
- Filtering and search should be designed with high-volume scenarios in mind
- Prefer virtualization-aware UI patterns for long log lists
- Be careful with `ObservableCollection<T>` churn; do not rebuild large UI collections unnecessarily when an incremental update or collection view refresh is clearer and cheaper

---

## File, type, and folder conventions

### File and type organization

- One public type per file
- File name should match the primary type name exactly
- Do not place unrelated classes, records, enums, and helpers into one file
- Avoid nesting helper types inside large classes unless the helper is truly tiny and private
- Split files when one class starts handling multiple responsibilities
- Keep option/configuration models, runtime state models, services, and UI models in separate files

### Naming and folder conventions

#### `SamLabs.Beobachter.Core`

- `Models`
- `Enums`
- `Filters`
- `Queries`
- `Interfaces`
- `Services`
- `Options`
- `State`

#### `SamLabs.Beobachter.Infrastructure`

- `Receivers`
- `Framing`
- `Parsing`
- `Persistence`
- `Configuration`
- `Platform`

#### `SamLabs.Beobachter.Application`

- `Features`
- `Views`
- `ViewModels`
- `Controls`
- `Converters`
- `Services`
- `Themes`
- `Resources`

#### `SamLabs.Beobachter.Tests`

- `Core`
- `Infrastructure`
- `Application`
- `Helpers`
- `TestData`

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

The UI may provide filter editors/builders and collection views, but the actual matching and filter semantics should stay outside the view layer.

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
- Prefer explicit load/save flows over hidden global mutable settings state

Suggested root location:

```text
%LOCALAPPDATA%/SamLabs.Beobachter/
```

Adjust exact filenames once the settings model is finalized.

---

## Testing guidance

Test `Core` aggressively.

Priority areas:

- parser behavior
- framing behavior
- filter matching
- query behavior
- log normalization
- session/log-store behavior
- malformed or partial log input
- receiver lifecycle state transitions
- receiver runtime state and failure reporting

Test `Infrastructure` where behavior is deterministic:

- parser implementations
- framing implementations
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
- Keep Avalonia-specific behavior in `Application`
- Prefer constructor injection
- Prefer CommunityToolkit.Mvvm source generators over handwritten boilerplate
- Do not leak Avalonia types into `Core`
- Do not put receiver, parser, framing, file, or network implementations in `Core`
- New features should usually begin with models/contracts in `Core`, then concrete implementations in `Infrastructure`, then UI wiring in `Application`

---

## Practical project intent

Beobachter is not a generic framework. It is a focused desktop product.

When making design decisions, prefer:

- clarity over cleverness
- maintainability over over-abstraction
- fast iteration over premature architecture
- modern .NET patterns over legacy desktop habits
- explicit boundaries over “shared utility” sprawl
- readability over dense or overly magical abstractions
