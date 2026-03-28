# Development Log

Purpose: keep an append-only record of what we changed and why, so design intent is preserved over time.

## How To Use This Log

- Add a new entry for every meaningful code/design change.
- Focus on intent and tradeoffs, not only file lists.
- Keep entries short but explicit.
- Do not rewrite old entries; append new ones.

## Entry Template

```md
## YYYY-MM-DD - Short Title
What changed:
- ...

Why:
- ...

Impact:
- ...

Follow-ups:
- ...
```

---

## 2026-03-28 - Phase 1 Foundation Wiring
What changed:
- Added a real composition root in App layer:
  [Root.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Composition/Root.cs)
- Wired startup through DI in:
  [App.axaml.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/App.axaml.cs)
- Added runtime theme service:
  [IThemeService.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/IThemeService.cs),
  [ThemeService.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/ThemeService.cs),
  [AppThemeMode.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/AppThemeMode.cs)
- Added centralized light/dark dictionaries:
  [Light.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Themes/Light.axaml),
  [Dark.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Themes/Dark.axaml),
  merged in [App.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/App.axaml).
- Updated shell VM/view for theme switching controls:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs),
  [MainWindow.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml).

Why:
- Establish a stable startup/composition baseline before deeper domain work.
- Ensure theming is centralized and not hardcoded per-view.
- Keep ViewModels DI-resolvable and ready for upcoming Core services.

Impact:
- App startup now uses service registration and service resolution.
- Runtime theme mode can be switched between `System`, `Light`, `Dark`.
- Foundation shell now reflects intended architecture direction.

Follow-ups:
- Replace placeholder shell text with real log workspace surfaces.
- Move from temporary VM defaults to persisted UI settings in later phase.

## 2026-03-28 - Avalonia Package Version Normalization
What changed:
- Unified Avalonia package versions to `11.3.12` in:
  [SamLabs.Beobachter.Application.csproj](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/SamLabs.Beobachter.Application.csproj)

Why:
- Mixed versions (`12.0.0-rc1` with `11.x` diagnostics availability) created restore/build instability.
- Stable, fully available package line was required for reliable iteration.

Impact:
- Restore/build became deterministic for the App project package graph.

Follow-ups:
- Re-evaluate Avalonia 12 migration later as a planned upgrade, not ad hoc.

## 2026-03-28 - Phase 2 Core Domain Baseline
What changed:
- Added canonical domain models and enums:
  [LogLevel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Enums/LogLevel.cs),
  [LogEntry.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Models/LogEntry.cs),
  [LogSourceContext.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Models/LogSourceContext.cs),
  [NormalizedLogLevel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Models/NormalizedLogLevel.cs)
- Added integer/string normalization table:
  [LogLevelTable.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Services/LogLevelTable.cs)
- Added logger trie structure:
  [LoggerNode.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Models/LoggerNode.cs)
- Added Core contracts for receivers/parsers/store/query:
  [ILogReceiver.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Interfaces/ILogReceiver.cs),
  [ILogParser.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Interfaces/ILogParser.cs),
  [ILogStore.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Interfaces/ILogStore.cs),
  [ILogQueryEvaluator.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Interfaces/ILogQueryEvaluator.cs),
  [LogQuery.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Queries/LogQuery.cs)
- Added in-memory store and query evaluator:
  [InMemoryLogStore.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Services/InMemoryLogStore.cs),
  [LogQueryEvaluator.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Services/LogQueryEvaluator.cs),
  [LogEntriesAppendedEventArgs.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Models/LogEntriesAppendedEventArgs.cs)

Why:
- Phase 2 goal is to define the shared domain truth before implementing receivers/UI behavior.
- Centralized normalization and logger hierarchy prevent logic drift across layers.
- Explicit contracts enforce `App -> Infrastructure -> Core` boundaries.

Impact:
- Core now contains concrete, testable primitives required for receiver/parser implementation.
- Infrastructure and tests can build against stable contracts.

Follow-ups:
- Add backpressure policy configuration object in Core options.
- Add metrics contracts for real-time summaries and time-bucketed statistics.

## 2026-03-28 - Test Project Modernization
What changed:
- Converted tests to xUnit with proper SDK/packages:
  [SamLabs.Beobachter.Tests.csproj](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/SamLabs.Beobachter.Tests.csproj)
- Added focused behavior tests:
  [LogLevelTableTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Core/LogLevelTableTests.cs),
  [LoggerNodeTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Core/LoggerNodeTests.cs)

Why:
- Lock key Core behavior (normalization and logger trie) before phase 3 parser/receiver work.

Impact:
- Current baseline: 17 passing tests for Phase 2 core behavior.

Follow-ups:
- Add tests for `InMemoryLogStore` query behavior and append notifications.
- Add characterization tests for log4j/NLog XML parsing during phase 3.
