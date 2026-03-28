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

## 2026-03-28 - Phase 3 Slice 1: Infrastructure XML Parser
What changed:
- Added first infrastructure parser implementation:
  [Log4jXmlParser.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Infrastructure/Parsing/Log4jXmlParser.cs)
- Parser implements Core contract:
  [ILogParser.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Interfaces/ILogParser.cs)
- Added parser characterization tests:
  [Log4jXmlParserTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Infrastructure/Parsing/Log4jXmlParserTests.cs)

Why:
- Phase 3 starts with a vertical slice that converts raw XML payloads into normalized `LogEntry` objects.
- Legacy compatibility required:
  - log4j/nlog namespace handling
  - integer and string level normalization through `LogLevelTable`
  - extraction of sequence, location info, properties, and exception fields
- Keeping this in `Infrastructure` preserves dependency direction (`Infrastructure -> Core`) and keeps parser logic out of App.

Impact:
- We can now parse representative log4j/NLog XML payloads into the new domain model.
- Parser behavior is test-locked before adding receivers.
- Test suite now covers Core + first Infrastructure behavior.

Follow-ups:
- Implement receiver integrations (`Udp`, `Tcp`, `File`) that feed this parser.
- Add CSV/plain-text parser implementations.
- Add malformed/partial input stress tests for parser robustness.

## 2026-03-28 - Phase 3 Slice 2: log4j2 XML Compatibility
What changed:
- Added architecture decision entry:
  [DECISION_LOG.md](/C:/Workspace/SamLabs.Beobachter/docs/DECISION_LOG.md)
- Extended parser implementation to support both legacy log4j/log4net-style XML and log4j2 `XmlLayout` events:
  [Log4jXmlParser.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Infrastructure/Parsing/Log4jXmlParser.cs)
- Added characterization tests for log4j2 shape (`Event`, `ContextMap`, `Source`, `Instant`, `Thrown`):
  [Log4jXmlParserTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Infrastructure/Parsing/Log4jXmlParserTests.cs)

Why:
- We cannot assume one “latest” schema in production pipelines; mixed emitters are common.
- Legacy compatibility remains required while modern log4j2 payloads must parse into the same normalized domain model.

Impact:
- One parser now accepts both schema families and maps them to the existing `LogEntry` contract.
- Receiver and app layers remain schema-agnostic.

Follow-ups:
- Add parser dispatch abstraction once CSV/plain-text parsers land.
- Add negative tests for malformed log4j2 payload fragments and namespace edge-cases.

## 2026-03-28 - Phase 3 Slice 3: UDP Receiver Vertical Slice
What changed:
- Added first channel-based receiver implementation:
  [UdpReceiver.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Infrastructure/Receivers/UdpReceiver.cs)
- Added typed receiver options:
  [UdpReceiverOptions.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Infrastructure/Receivers/UdpReceiverOptions.cs)
- Added integration-style loopback tests:
  [UdpReceiverTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Infrastructure/Receivers/UdpReceiverTests.cs)

Why:
- Phase 3 requires concrete receiver loops that write normalized entries into `ChannelWriter<LogEntry>`.
- UDP is the smallest network receiver slice to validate lifecycle (`Start`/`Stop`) and parser integration end-to-end.

Impact:
- Infrastructure now has a working receiver that:
  - binds to configured address/port
  - parses datagrams through `ILogParser`
  - writes parsed entries to the injected channel
  - handles cancellation and disposal safely
- Test suite increased to 25 passing tests.

Follow-ups:
- Implement `TcpReceiver` with equivalent lifecycle guarantees.
- Implement file-tail receiver and shared parser dispatch for XML/CSV/plain text.

## 2026-03-28 - Phase 3 Slice 4: TCP Receiver + XML Framing
What changed:
- Added TCP receiver options and implementation:
  [TcpReceiverOptions.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Infrastructure/Receivers/TcpReceiverOptions.cs),
  [TcpReceiver.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Infrastructure/Receivers/TcpReceiver.cs)
- Added shared XML event framing utility for stream-based receivers:
  [XmlEventFrameExtractor.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Infrastructure/Parsing/XmlEventFrameExtractor.cs)
- Added integration-style TCP tests:
  [TcpReceiverTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Infrastructure/Receivers/TcpReceiverTests.cs)

Why:
- TCP streams carry concatenated payloads; receiver must frame complete XML events before parser invocation.
- Framing logic is now reusable between TCP and file-tail ingestion to avoid divergent behavior.

Impact:
- `TcpReceiver` now accepts connections, reads stream payloads, extracts complete log events, parses, and writes to `ChannelWriter<LogEntry>`.
- Lifecycle and multi-event behavior are covered by tests.

Follow-ups:
- Extend framing strategy later if we need additional payload formats on TCP (for example line-delimited plain text).

## 2026-03-28 - Phase 3 Slice 5: File Tail Receiver
What changed:
- Added file-tail receiver options and implementation:
  [FileTailReceiverOptions.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Infrastructure/Receivers/FileTailReceiverOptions.cs),
  [FileTailReceiver.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Infrastructure/Receivers/FileTailReceiver.cs)
- Added file-tail integration tests:
  [FileTailReceiverTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Infrastructure/Receivers/FileTailReceiverTests.cs)

Why:
- File ingestion is one of the MVP parity receiver paths and needs the same channel-based flow as network receivers.
- Tail loop includes truncation handling and bounded in-memory buffer to stay robust when files rotate or payloads are partial.

Impact:
- Infrastructure now includes `Udp`, `Tcp`, and `FileTail` receivers feeding normalized `LogEntry` objects into channels.
- Receiver test coverage now verifies all three ingest modes.

Follow-ups:
- Add parser composition for CSV/plain-text and wire receiver-specific parser chains.
- Add stress/cancellation tests under sustained ingest for receiver loops.

## 2026-03-28 - Phase 3 Slice 6: Parser Composition (XML/CSV/Plain Text)
What changed:
- Added parser composition layer:
  [CompositeLogParser.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Infrastructure/Parsing/CompositeLogParser.cs)
- Added CSV parser and options:
  [CsvParser.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Infrastructure/Parsing/CsvParser.cs),
  [CsvParserOptions.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Infrastructure/Parsing/CsvParserOptions.cs)
- Added plain text parser:
  [PlainTextParser.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Infrastructure/Parsing/PlainTextParser.cs)
- Added coverage tests:
  [CompositeLogParserTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Infrastructure/Parsing/CompositeLogParserTests.cs),
  [CsvParserTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Infrastructure/Parsing/CsvParserTests.cs),
  [PlainTextParserTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Infrastructure/Parsing/PlainTextParserTests.cs)

Why:
- Receivers must support heterogeneous payloads without duplicating parse-selection logic.
- CSV/plain-text support was still a known parity gap in phase 3.

Impact:
- Infrastructure can now chain parsers by priority and stop on first successful parse.
- File and network receiver paths can share the same parse pipeline abstraction.
- Test suite increased to 38 passing tests.

Follow-ups:
- Wire receiver-specific parser chains through typed receiver definitions/settings.
- Add malformed CSV and quoted multiline payload edge-case tests.

## 2026-03-28 - Phase 3 Slice 7: Typed Settings + Receiver Factory Wiring
What changed:
- Added split typed settings records in Core:
  [AppSettings.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Settings/AppSettings.cs),
  [ReceiverDefinitions.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Settings/ReceiverDefinitions.cs),
  [WorkspaceSettings.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Settings/WorkspaceSettings.cs),
  [UiLayoutSettings.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Settings/UiLayoutSettings.cs)
- Added Core settings persistence contract:
  [ISettingsStore.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Interfaces/ISettingsStore.cs)
- Added source-generated JSON settings store in Infrastructure:
  [JsonSettingsStore.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Infrastructure/Settings/JsonSettingsStore.cs),
  [SettingsJsonContext.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Infrastructure/Settings/SettingsJsonContext.cs),
  [JsonSettingsStoreOptions.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Infrastructure/Settings/JsonSettingsStoreOptions.cs)
- Added parser pipeline + receiver materialization wiring from typed definitions:
  [ParserPipelineFactory.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Infrastructure/Parsing/ParserPipelineFactory.cs),
  [ReceiverFactory.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Infrastructure/Receivers/ReceiverFactory.cs)
- Added tests:
  [JsonSettingsStoreTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Infrastructure/Settings/JsonSettingsStoreTests.cs),
  [ParserPipelineFactoryTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Infrastructure/Parsing/ParserPipelineFactoryTests.cs),
  [ReceiverFactoryTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Infrastructure/Receivers/ReceiverFactoryTests.cs)

Why:
- Phase 3 needed explicit typed settings split and deterministic parser selection per receiver definition.
- This closes the gap between configuration data and concrete receiver/parser runtime objects.

Impact:
- Settings now persist as four JSON files with source-generated serializers.
- Receiver creation can now be driven by typed config with parser-order control.
- Test suite increased to 43 passing tests.

Follow-ups:
- Integrate settings store and receiver factory into App composition/session lifecycle.
- Add migration shim only if legacy settings import is required.

## 2026-03-28 - Phase 4 Slice 1: Ingestion Session + App Wiring
What changed:
- Added app-level ingestion session contract and implementations:
  [IIngestionSession.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/IIngestionSession.cs),
  [IngestionSession.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/IngestionSession.cs),
  [DesignIngestionSession.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/DesignIngestionSession.cs)
- `IngestionSession` now owns:
  - bounded `Channel<LogEntry>` ingestion (`DropOldest`)
  - dropped-count telemetry counter
  - batch consumer loop into `InMemoryLogStore`
  - receiver startup/shutdown via typed settings + `ReceiverFactory`
- Wired App DI composition for parsing/receiver/settings/session services:
  [Root.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Composition/Root.cs)
- Wired startup/shutdown lifecycle to start/stop ingestion session:
  [App.axaml.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/App.axaml.cs)

Why:
- Phase 4 requires a single app-level composition point that owns ingestion flow and batches data before UI binding.
- Session lifecycle must be explicit and independent from individual view models.

Impact:
- App runtime now has a concrete channel-based ingestion session and receiver lifecycle host.
- Store append events are now available as a stable UI update surface.

Follow-ups:
- Bind MainWindow VM to live session snapshots and append events.
- Add filtering/search summary and visible-list virtualization constraints in VM/view.

## 2026-03-28 - Phase 4 Slice 2: Live Main Window Log Surface
What changed:
- Replaced shell placeholder VM with live session-bound VM:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
- VM now:
  - subscribes to `IIngestionSession` append events
  - maintains bounded observable visible list (`MaxVisibleEntries`)
  - supports live text filtering
  - surfaces session status summary (`Total`, `Visible`, `Dropped`)
  - provides sample ingest command for immediate validation without external senders
- Reworked main view to log-workspace layout:
  [MainWindow.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml)
  - top status/theme bar
  - search + actions row
  - columned log list bound to live entries

Why:
- Phase 4 requires replacing static shell text with an actual UI surface connected to ingestion/session state.
- This provides an immediate end-to-end vertical path: publish -> channel -> batch store -> UI update.

Impact:
- The app now renders and updates a real log list with filtering and telemetry summary.
- Sample entries can be generated from UI to verify data flow and theming without receiver setup.

Follow-ups:
- Add pause/resume ingestion controls and logger-tree binding to `LoggerNode`.
- Add richer details pane (properties/exception/source fields) and level toggles.

## 2026-03-28 - Phase 4 Slice 3: Pause/Resume Ingestion State
What changed:
- Extended ingestion contract with explicit pause state and control:
  [IIngestionSession.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/IIngestionSession.cs)
- Implemented pause behavior in session consumer loop and persisted pause flag via workspace settings:
  [IngestionSession.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/IngestionSession.cs)
- Updated design-time session stub with pause support:
  [DesignIngestionSession.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/DesignIngestionSession.cs)
- Added pause toggle surface in VM and UI:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs),
  [MainWindow.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml)

Why:
- Phase 4 required ingest flow state controls rather than a passive always-on pipeline.
- Persisting pause in `WorkspaceSettings` keeps user intent across restarts.

Impact:
- Ingestion session now supports `Pause`/`Resume` without tearing down receiver lifecycle.
- UI status and control state now expose runtime flow state directly.

Follow-ups:
- Add logger tree filtering and details pane to complete parity-oriented workspace interactions.

## 2026-03-28 - Phase 4 Slice 4: Logger Tree Bound to Core `LoggerNode`
What changed:
- Added logger tree item VM wrapper bound to `Core.LoggerNode`:
  [LoggerTreeItemViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/LoggerTreeItemViewModel.cs)
- Updated main VM to:
  - maintain logger trie state from session snapshots/appends
  - expose hierarchical logger tree items
  - apply logger enabled/disabled state in the same filter path as visible logs
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
- Updated UI to render tree with hierarchical checkboxes:
  [MainWindow.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml)

Why:
- Logger grouping/filtering must be driven by the shared Core trie, not reimplemented in UI-specific path logic.
- This closes a key parity behavior from the legacy app while preserving current architecture boundaries.

Impact:
- Logger enable/disable is now interactive and immediately affects visible entries.
- Tree state is derived from live ingestion data and remains aligned with domain model semantics.

Follow-ups:
- Add details pane with selected-entry fields and copy actions.
- Add level toggle controls integrated with existing filter path.

## 2026-03-28 - Phase 4 Slice 5: Details Pane, Level Toggles, and VM Tests
What changed:
- Added clipboard abstraction + runtime implementation:
  [IClipboardService.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/IClipboardService.cs),
  [AvaloniaClipboardService.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/AvaloniaClipboardService.cs),
  [NullClipboardService.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/NullClipboardService.cs)
- Wired clipboard service in composition root:
  [Root.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Composition/Root.cs)
- Extended main VM with:
  - selected entry + formatted details text
  - copy-message/copy-details commands
  - level toggles (`Trace`..`Fatal`) in the same filter path as search/logger filters
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
- Extended UI with:
  - level toggle toolbar
  - selected-entry details pane
  - copy actions and copy-status feedback
  [MainWindow.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml)
- Added app-level VM tests and project reference:
  [MainWindowViewModelTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowViewModelTests.cs),
  [SamLabs.Beobachter.Tests.csproj](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/SamLabs.Beobachter.Tests.csproj)

Why:
- Phase 4 needed the details/copy workflow and level-based filtering to move from a raw feed viewer to a usable inspection workspace.
- VM tests are required to lock behavior around state transitions and filter interactions.

Impact:
- The workspace now supports selected log inspection, clipboard copy flows, and combined filtering by level/logger/text.
- Automated tests now include main VM behavior (filters, pause toggle, clipboard integration).
- Test suite increased to 46 passing tests.

Follow-ups:
- Add explicit auto-scroll/pin-to-bottom behavior controls.
- Introduce virtualization-aware list strategy for very high entry counts.

## 2026-03-28 - Phase 4 Slice 6: Pin-to-Bottom Auto-Scroll + Explicit Virtualization
What changed:
- Extended ingestion session contract with persisted auto-scroll state:
  [IIngestionSession.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/IIngestionSession.cs),
  [IngestionSession.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/IngestionSession.cs),
  [DesignIngestionSession.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/DesignIngestionSession.cs)
- Updated main VM with auto-scroll pin state and toggle command:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
- Added view-level pin-to-bottom behavior:
  [MainWindow.axaml.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml.cs)
  - subscribes to visible-entry collection changes
  - scrolls to latest only when pin is enabled
- Made list virtualization explicit in XAML:
  [MainWindow.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml)
  - named log list for scroll targeting
  - `VirtualizingStackPanel` with cache length
- Added VM test coverage for auto-scroll toggle:
  [MainWindowViewModelTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowViewModelTests.cs)

Why:
- Auto-scroll needed explicit user control so operators can inspect historical rows without fighting live ingest.
- Explicit virtualization configuration reduces rendering pressure as visible rows grow.

Impact:
- Workspace now supports predictable pin-to-bottom behavior (`Pin: On/Off`).
- Auto-scroll preference is persisted via workspace settings through ingestion session.
- Test suite increased to 47 passing tests.

Follow-ups:
- Add optional "only pin if user already near bottom" nuance.
- Add high-volume UI perf benchmark harness for batching/virtualization tuning.

## 2026-03-28 - Phase 4 Slice 7: Near-Bottom Auto-Scroll Nuance
What changed:
- Refined `MainWindow` auto-scroll behavior to scroll to latest only when the user is already near the bottom:
  [MainWindow.axaml.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml.cs)
  - resolves internal list `ScrollViewer`
  - checks distance-to-bottom threshold before auto-scroll on new entries
  - keeps `Pin` control semantics and still jumps to latest when pin is toggled on

Why:
- Always-forced auto-scroll while pinned can disrupt investigation when the user intentionally scrolls up a short distance.
- Near-bottom gating preserves operator control while still behaving like a live tail when appropriate.

Impact:
- Pinned mode now behaves more predictably during active inspection.
- Build/tests remain green with existing VM coverage.

Follow-ups:
- Optionally expose the near-bottom threshold as an advanced setting if needed for different display densities.

## 2026-03-28 - Phase 4 Slice 8: UI Behavior Pass (Severity, Density, Shortcuts)
What changed:
- Added severity row brushes in theme dictionaries:
  [Light.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Themes/Light.axaml),
  [Dark.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Themes/Dark.axaml)
- Added log-level-to-row-brush converter:
  [LogLevelToRowBrushConverter.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Converters/LogLevelToRowBrushConverter.cs)
- Updated main view to:
  - render severity-colored rows
  - support density toggle button and row metrics binding
  - add explicit search box name and window key handling hook
  [MainWindow.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml)
- Updated view code-behind for keyboard shortcuts:
  [MainWindow.axaml.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml.cs)
  - `Ctrl+F` focuses search
  - `Ctrl+C` copies selected details
  - `Up/Down` navigates selected log row outside text input
- Updated VM with density state/command and row metrics:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
- Added VM test for density behavior:
  [MainWindowViewModelTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowViewModelTests.cs)

Why:
- This closes the UI behavior gap for readability (severity cues), operator ergonomics (row density), and keyboard-driven workflows.

Impact:
- Log list is easier to scan under load and usable without mouse-heavy interaction.
- Test suite increased to 48 passing tests.

Follow-ups:
- Add configurable keymap if custom shortcuts become a requirement.
