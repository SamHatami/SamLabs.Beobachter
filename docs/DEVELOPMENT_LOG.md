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

## 2026-03-28 - Phase 4 Slice 9: Receiver Setup UI + Session Reload Hook
What changed:
- Extended ingestion session API to support receiver reload without full session restart:
  [IIngestionSession.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/IIngestionSession.cs),
  [IngestionSession.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/IngestionSession.cs),
  [DesignIngestionSession.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/DesignIngestionSession.cs)
- Added design-time settings store for VM fallback:
  [DesignSettingsStore.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/DesignSettingsStore.cs)
- Added receiver-definition editor VM model:
  [ReceiverDefinitionViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/ReceiverDefinitionViewModel.cs)
- Extended main VM with receiver setup actions:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
  - load receiver definitions from settings
  - add/remove UDP/TCP/File receiver drafts
  - save + reload listeners
  - reload definitions from disk
- Added receiver setup panel in the main workspace:
  [MainWindow.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml)
- Added VM tests for receiver setup flows:
  [MainWindowViewModelTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowViewModelTests.cs)

Why:
- Legacy Log2Console required receiver/listener setup; this restores that operational flow in the new MVVM shell.
- Reloading receivers in-place avoids full app restart and keeps stateful UI workflows intact.
- Explicit tests lock persistence/reload behavior before moving into structured logging support.

Impact:
- Users can now define listeners in-app (UDP/TCP/File), persist settings, and reload active receivers from the UI.
- Session lifecycle remains stable while receiver topology changes.
- Test suite increased to 50 passing tests.

Follow-ups:
- Add per-receiver parser-order editing and validation messages in the setup panel.

## 2026-03-28 - Phase 3 Slice 8: Structured JSON Parser
What changed:
- Added JSON parser implementation:
  [JsonLogParser.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Infrastructure/Parsing/JsonLogParser.cs)
  - supports common structured shapes (`timestamp/time/@t`, `level/@l`, `message/@m`, `logger/SourceContext`)
  - supports numeric and string log-level normalization through `LogLevelTable`
  - maps JSON property bags and extra root fields into `LogEntry.Properties`
- Wired JSON parser into app parser registration:
  [Root.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Composition/Root.cs)
- Updated default receiver parser order to include JSON:
  [ReceiverDefinitions.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Settings/ReceiverDefinitions.cs)
- Added parser coverage tests:
  [JsonLogParserTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Infrastructure/Parsing/JsonLogParserTests.cs)
- Extended composition/factory tests to include JSON parser in real parser chains:
  [CompositeLogParserTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Infrastructure/Parsing/CompositeLogParserTests.cs),
  [ReceiverFactoryTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Infrastructure/Receivers/ReceiverFactoryTests.cs)

Why:
- Structured logging payloads are common in modern stacks (Serilog/NLog/logback JSON layouts).
- We need schema-flexible ingestion so operators can use the same viewer across XML, CSV, plain text, and JSON senders.

Impact:
- Receiver pipelines can now parse structured JSON payloads without custom receiver code.
- Structured fields are preserved into `Properties` for filtering/details workflows.
- Test suite increased to 55 passing tests.

Follow-ups:
- Add optional parser-specific settings for strict vs permissive JSON field-name matching.

## 2026-03-28 - Phase 4 Slice 10: Structured Payload Preservation in Domain/UI
What changed:
- Extended core log model for structured logging fidelity:
  [LogEntry.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Models/LogEntry.cs)
  - added `MessageTemplate`
  - added `StructuredPayloadJson`
- Updated structured JSON parser to populate new fields:
  [JsonLogParser.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Infrastructure/Parsing/JsonLogParser.cs)
  - reads template fields (for example `@mt`)
  - preserves original structured JSON payload text
- Updated details-pane formatter to expose structured metadata:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
- Added tests for parser/domain and UI details rendering:
  [JsonLogParserTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Infrastructure/Parsing/JsonLogParserTests.cs),
  [MainWindowViewModelTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowViewModelTests.cs)

Why:
- String-only message rendering loses key structured context (templates + full JSON body) needed for root-cause analysis and downstream statistics work.
- Preserving structured payload in the domain model keeps options open for future typed field indexing without parser rewrites.

Impact:
- Structured logs now retain template and raw JSON payload in `LogEntry`.
- Details pane includes structured payload content for inspection/copy workflows.
- Test suite increased to 56 passing tests.

Follow-ups:
- Add query helpers for common structured keys (for example `tenant`, `traceId`, `spanId`) on top of the preserved payload.

## 2026-03-28 - Phase 4 Slice 11: Structured Filter/Query Pass
What changed:
- Extended query object in Core for structured filtering:
  [LogQuery.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Queries/LogQuery.cs)
  - added `LoggerContains`, `ThreadContains`, and `PropertyContains`
- Extended query evaluator behavior:
  [LogQueryEvaluator.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Services/LogQueryEvaluator.cs)
  - free-text fallback now checks message, logger, exception, thread, and property keys/values
  - structured filters now support receiver/logger/thread/field matches and minimum level
- Updated main workspace VM to use query-object composition for filtering:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
  - added field filters (`Receiver`, `Logger`, `Thread`, `Tenant`, `TraceId`)
  - added minimum-level selector and clear-fields command
  - moved filtering to a single `BuildCurrentQuery` path
- Updated main view with a structured filter bar:
  [MainWindow.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml)
- Added tests:
  [LogQueryEvaluatorTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Core/LogQueryEvaluatorTests.cs),
  [MainWindowViewModelTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowViewModelTests.cs)

Why:
- As filter axes grew, ad hoc checks in the VM were becoming brittle and harder to reason about.
- `LogQuery` now acts as a clear query object boundary between UI intent and match semantics.

Impact:
- Operators can now narrow logs by receiver/logger/thread and key structured fields without losing free-text search.
- Filter logic is centralized, testable, and easier to extend.
- Test suite increased to 59 passing tests.

Follow-ups:
- Add an advanced filter mode for arbitrary property key/value entries beyond `tenant` and `traceId`.

## 2026-03-28 - Phase 4 Slice 12: Rolling Statistics Service + Live Summary Panel
What changed:
- Added rolling statistics contracts/models:
  [ILogStatisticsService.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/ILogStatisticsService.cs),
  [LogStatisticsSnapshot.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/LogStatisticsSnapshot.cs)
- Added rolling 1-second bucket aggregator:
  [RollingLogStatisticsService.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/RollingLogStatisticsService.cs)
  - 1-minute and 5-minute throughput/error rates
  - top loggers and top receivers in 5-minute window
  - automatic stale-bucket trimming
- Wired stats service in composition root:
  [Root.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Composition/Root.cs)
- Updated main VM to record appended batches and project live summaries:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
- Updated main view to show live stats in header:
  [MainWindow.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml)
- Added unit tests:
  [RollingLogStatisticsServiceTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/RollingLogStatisticsServiceTests.cs)

Why:
- Real-time ops workflows need trend visibility, not only raw line-by-line logs.
- A dedicated rolling aggregator avoids mixing stats math into VM/UI event handlers.

Impact:
- Header now displays live 1m/5m log/error rates and top noisy logger/receiver sources.
- Stats logic is isolated and testable as a standalone service.
- Test suite increased to 61 passing tests.

Follow-ups:
- Add chart-ready time-series output from the same buckets for a future visual trend panel.

## 2026-03-28 - Phase 4 Slice 13: Receiver Setup Hardening + Parser Order Editing
What changed:
- Extended receiver editor model with parser-order text:
  [ReceiverDefinitionViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/ReceiverDefinitionViewModel.cs)
- Extended main VM receiver setup flow with:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
  - validation for duplicate/empty IDs
  - validation for display name, port range, bind address, and file path/poll interval
  - parser-order parsing and known-parser validation
  - parser-order mapping to/from typed receiver settings
- Updated receiver setup UI with parser-order editor field:
  [MainWindow.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml)
- Extended VM tests:
  [MainWindowViewModelTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowViewModelTests.cs)
  - invalid configuration blocks save/reload
  - parser-order edits persist into receiver settings

Why:
- Receiver setup is an operational control plane; invalid values should fail fast before touching running listeners.
- Parser selection is now explicit per receiver and no longer hidden behind defaults.

Impact:
- Save now guards against malformed receiver config and surfaces clear validation status.
- Operators can configure parser order directly in UI with round-trip persistence.
- Test suite increased to 62 passing tests.

Follow-ups:
- Add inline field-level validation visuals (not only status text) in the setup panel.

## 2026-03-28 - Phase 4 Slice 14: Workspace/UI Persistence Completion
What changed:
- Extended workspace settings model to persist filter/density/profile state:
  [WorkspaceSettings.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Settings/WorkspaceSettings.cs)
  - receiver/logger/thread/tenant/trace filters
  - minimum-level option
  - compact density flag
  - selected receiver profile id
- Extended UI layout settings model to persist log column widths:
  [UiLayoutSettings.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Core/Settings/UiLayoutSettings.cs)
- Updated main VM with debounced persistence workflow:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
  - load workspace/ui settings on startup
  - restore selected receiver profile
  - save workspace/ui state on relevant UI changes (filters, levels, density, selected profile, column widths)
  - added column width commands (`Col -`, `Col +`, `Col Reset`)
- Updated main view bindings:
  [MainWindow.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml)
  - bound log list/header column widths to persisted VM properties
  - added quick controls for column width adjustment
- Extended app VM tests for restore/persist behavior:
  [MainWindowViewModelTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowViewModelTests.cs)

Why:
- Session continuity is a core productivity requirement for a log inspector.
- State persistence is now explicit and centralized instead of incidental in ad hoc view-model defaults.

Impact:
- Restarting the app now restores filter context, density mode, selected receiver profile, and log column widths.
- UI layout and workspace state are saved incrementally with debounce to avoid excessive disk writes.
- Test suite increased to 65 passing tests.

Follow-ups:
- Persist actual window geometry/maximized state through app lifetime hooks (currently model supports it, but the shell does not wire runtime updates yet).

## 2026-03-28 - Phase 4 Slice 15: Inline Receiver Field Validation
What changed:
- Extended receiver editor VM with field-level validation state:
  [ReceiverDefinitionViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/ReceiverDefinitionViewModel.cs)
  - added per-field validation messages/flags for display name, id, bind address, port, file path, poll interval, and parser order
- Extended receiver setup orchestration to maintain live validation:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
  - validates all receiver entries on add/remove/edit and on save
  - keeps duplicate-id validation synchronized across entries
  - attaches/detaches receiver item change handlers explicitly
- Updated receiver setup UI with inline validation visuals:
  [MainWindow.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml)
  - invalid fields now get red border styling
  - field-specific error text is shown directly below relevant inputs
- Added validation brush theme tokens:
  [Light.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Themes/Light.axaml),
  [Dark.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Themes/Dark.axaml)
- Added/updated VM tests for inline validation behavior:
  [MainWindowViewModelTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowViewModelTests.cs)

Why:
- Validation feedback only appeared as a single status message during save, which made receiver setup slower and error-prone.
- Receiver setup is an operational surface; users need immediate, field-local guidance before applying changes.

Impact:
- Receiver setup now provides immediate field-level validation markers/messages without waiting for save.
- Invalid save attempts still fail, but with synchronized inline hints on the exact failing fields.
- Existing build/test verification is currently blocked by unrelated compile issues already present in `Core` interfaces.

Follow-ups:
- Add inline validation visibility tests at the view layer if UI test coverage is introduced for setup workflows.

## 2026-03-28 - MVVM Refactor Phase 0: Baseline Test Segmentation
What changed:
- Split the monolithic `MainWindowViewModel` test class into focused files:
  [MainWindowFilteringTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowFilteringTests.cs),
  [MainWindowSessionAndDetailsTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowSessionAndDetailsTests.cs),
  [MainWindowReceiverSetupTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowReceiverSetupTests.cs),
  [MainWindowWorkspaceStateTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowWorkspaceStateTests.cs)
- Added shared test support and doubles:
  [MainWindowTestSupport.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowTestSupport.cs)
- Removed the previous single-file test container:
  [MainWindowViewModelTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowViewModelTests.cs)

Why:
- The upcoming shell decomposition will move responsibilities into child ViewModels; keeping one large test class would make the migration brittle.
- Segmenting by behavior surface makes it possible to move tests with each extracted VM while preserving expected behavior.

Impact:
- No runtime behavior changed.
- App-level VM tests now align with planned feature boundaries (filters, session/details, receiver setup, workspace state).

Follow-ups:
- During each extraction phase, move the matching tests from shell-oriented names to feature VM-specific test classes.

## 2026-03-28 - MVVM Refactor Phase 1A: Entry Details Extraction
What changed:
- Extracted details projection and copy behavior into:
  [EntryDetailsViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/EntryDetailsViewModel.cs)
  - owns selected entry projection (`SelectedDetailsText`)
  - owns copy status and copy commands
  - owns details text formatting
- Updated shell VM to compose details VM:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
  - added `Details` child VM
  - removed direct details/copy command implementation
  - added temporary shell pass-through properties/commands (`SelectedEntry`, `SelectedDetailsText`, `CopyStatus`, copy commands) for compatibility during phased migration
- Added focused tests for the new child VM:
  [EntryDetailsViewModelTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/EntryDetailsViewModelTests.cs)

Why:
- Details projection/copy logic is a distinct panel responsibility and should not remain in the shell VM.
- A compatibility bridge allows extraction now without forcing broad XAML/code-behind churn in the same commit.

Impact:
- Behavior is unchanged in the current UI surface.
- `MainWindowViewModel` now delegates details-specific state and commands to a dedicated child VM.
- Test suite increased to 68 passing tests.

Follow-ups:
- Rebind details area and keyboard handlers directly to `Details` in a later shell-cleanup phase and remove pass-throughs.

## 2026-03-28 - MVVM Refactor Phase 1B: Filters Extraction
What changed:
- Extracted filter state/query construction into:
  [LogFiltersViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/LogFiltersViewModel.cs)
  - owns text/structured/min-level filters
  - owns level toggles and filter-reset commands
  - owns `LogQuery` projection and enabled-level snapshot behavior
- Updated shell VM to compose filters VM:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
  - added `Filters` child VM
  - removed direct filter/level state fields and related command handlers
  - delegated query and level evaluation to `Filters`
  - added temporary pass-through properties/commands to preserve current bindings while migration continues
- Added focused child-VM tests:
  [LogFiltersViewModelTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/LogFiltersViewModelTests.cs)

Why:
- Filtering is a bounded UI/domain projection surface and should be owned by a dedicated VM rather than the shell.
- This extraction isolates query-related state transitions before stream/source-tree extraction.

Impact:
- Visible filter behavior in the current UI remains unchanged.
- Shell now coordinates filter changes via child-VM events instead of property partial methods.
- Test suite increased to 71 passing tests.

Follow-ups:
- Move XAML bindings from shell filter pass-throughs to direct `Filters.*` bindings and remove wrapper properties in shell cleanup phase.

## 2026-03-28 - MVVM Refactor Phase 1C: Receiver Setup Extraction
What changed:
- Extracted receiver setup surface into:
  [ReceiverSetupViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/ReceiverSetupViewModel.cs)
  - owns receiver definition collection
  - owns add/remove/save/reload commands
  - owns load/apply mapping from settings
  - owns validation and parser-order normalization logic
- Updated shell VM to compose receiver setup VM:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
  - removed direct receiver setup command/validation/mapping implementation
  - added `ReceiverSetup` child VM with compatibility pass-throughs (`ReceiverDefinitions`, selected receiver, setup status, setup commands)
  - replaced direct receiver-definition loading with child-VM `LoadAsync()` orchestration and pending selection application
- Added focused child-VM tests:
  [ReceiverSetupViewModelTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/ReceiverSetupViewModelTests.cs)

Why:
- Receiver setup is a full feature surface with independent lifecycle/validation and should not be embedded in shell orchestration code.
- Extracting this now isolates the highest-churn setup logic before stream and source-tree refactors.

Impact:
- Current UI behavior remains unchanged through shell pass-through bindings.
- Receiver setup responsibilities now live behind a dedicated VM boundary.
- Test suite increased to 75 passing tests.

Follow-ups:
- Rebind receiver setup panel directly to `ReceiverSetup.*` and remove pass-through properties during shell cleanup.

## 2026-03-28 - MVVM Refactor Phase 2: Log Stream Extraction
What changed:
- Extracted stream surface into:
  [LogStreamViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/LogStreamViewModel.cs)
  - owns visible entry collection and selected row
  - owns append/rebuild entry projection with max-visible cap
  - owns density and column-width state/commands
- Updated shell VM to compose stream VM:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
  - removed direct stream collection mutation and row-density/column-width implementation
  - delegated append/rebuild operations to `Stream`
  - bridged stream selected row to `Details.SelectedEntry`
  - kept compatibility pass-throughs for existing bindings (`VisibleEntries`, `SelectedEntry`, density/column properties and commands)
- Added focused stream tests:
  [LogStreamViewModelTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/LogStreamViewModelTests.cs)

Why:
- Stream virtualization-facing state and projection logic are independent responsibilities that should not live in shell orchestration.
- This extraction removes one of the largest mutable-state clusters from `MainWindowViewModel`.

Impact:
- UI behavior remains unchanged at current bindings.
- Stream append/rebuild and row/column behavior now live in a dedicated VM boundary.
- Test suite increased to 79 passing tests.

Follow-ups:
- Rebind log surface controls directly to `Stream.*` and drop pass-through stream properties during shell cleanup.

## 2026-03-28 - MVVM Refactor Phase 3: Source Tree Extraction
What changed:
- Extracted source tree behavior into:
  [SourceTreeViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/SourceTreeViewModel.cs)
  - owns logger root and tree item collection
  - owns logger registration and snapshot rebuild
  - owns logger enable/disable and "enable all" command
  - emits state-change event for shell coordination
- Updated shell VM to compose source tree VM:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
  - removed direct logger-tree root/state ownership
  - delegated logger registration and enabled checks to `Sources`
  - replaced shell `EnableAllLoggers` command implementation with pass-through to `Sources`
  - coordinated source-tree state changes via `Sources.StateChanged`
- Added focused source-tree tests:
  [SourceTreeViewModelTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/SourceTreeViewModelTests.cs)

Why:
- Source tree state is a dedicated panel concern and should be isolated from shell responsibilities.
- This extraction removes another substantial behavior cluster from `MainWindowViewModel`.

Impact:
- Existing UI behavior remains stable with compatibility pass-throughs.
- Logger tree registration and toggle state now live behind a dedicated VM boundary.
- Test suite increased to 82 passing tests.

Follow-ups:
- Move source panel bindings directly to `Sources.*` and remove shell pass-throughs in final shell cleanup.

## 2026-03-28 - MVVM Refactor Phase 4: Shell Pass-Through Removal + Binding Realignment
What changed:
- Removed remaining shell pass-through members from:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
  - deleted wrapper properties/commands for filters, stream, details, receiver setup, and source tree
  - kept only child VM composition (`Filters`, `Stream`, `Details`, `ReceiverSetup`, `Sources`) and shell orchestration
  - updated workspace/layout persistence and status summary paths to read/write through child VMs
- Rebound the main window to child VMs directly:
  [MainWindow.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml)
  - switched bindings to `Filters.*`, `Stream.*`, `Details.*`, `ReceiverSetup.*`, and `Sources.*`
- Updated view code-behind interactions to use child VMs without setting `DataContext` in code:
  [MainWindow.axaml.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml.cs)
- Updated shell-level tests to target child VM surfaces directly:
  [MainWindowFilteringTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowFilteringTests.cs),
  [MainWindowReceiverSetupTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowReceiverSetupTests.cs),
  [MainWindowSessionAndDetailsTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowSessionAndDetailsTests.cs),
  [MainWindowWorkspaceStateTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowWorkspaceStateTests.cs),
  [MainWindowTestSupport.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowTestSupport.cs)

Why:
- The shell still contained compatibility wrappers that kept coupling high and made refactor regressions harder to reason about.
- Direct binding to child VMs matches the intended shell-composition architecture while keeping ViewLocator/DataContext ownership intact.

Impact:
- `MainWindowViewModel` is now materially smaller and focused on coordination instead of panel state ownership.
- Main window bindings now reflect real VM boundaries.
- Full test suite remains green (`82` passing tests).

Follow-ups:
- If desired, split remaining shell-only concerns (top bar status/theme controls) into dedicated toolbar/status child VMs.

## 2026-03-28 - MVVM Refactor Phase 5A: UI Surface Composition (Toolbar, Quick Filters, Session Health)
What changed:
- Added three new child ViewModels aligned with the refactor skeleton:
  [MainToolbarViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/Toolbar/MainToolbarViewModel.cs),
  [QuickFiltersViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/Sources/QuickFiltersViewModel.cs),
  [SessionHealthViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/Status/SessionHealthViewModel.cs)
- Added matching views for ViewLocator composition:
  [MainToolbarView.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/Toolbar/MainToolbarView.axaml),
  [QuickFiltersView.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/Sources/QuickFiltersView.axaml),
  [SessionHealthView.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/Status/SessionHealthView.axaml)
- Updated shell VM composition/orchestration:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
  - composed `Toolbar`, `QuickFilters`, and `SessionHealth`
  - added quick-filter criteria handling (`Errors and above`, `Structured only`) in filtering path
  - added quick-filter count projection from ingestion snapshot
  - added session-health summary projection (active receivers, buffered entries, structured events, dropped packets)
- Updated shell view composition:
  [MainWindow.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml)
  - replaced top inline header block with `ContentControl` bound to `Toolbar`
  - added `QuickFilters` panel below source tree
  - added `SessionHealth` panel below details area
  - removed duplicated pause/auto-scroll/density buttons from filter/action row (now in toolbar)
- Extended filtering tests for the new quick-filter behaviors:
  [MainWindowFilteringTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowFilteringTests.cs)

Why:
- The skeleton refactor calls for explicit UI surface composition through child VMs/views and ViewLocator, instead of one large inlined window surface.
- Introducing these surfaces now keeps shell coordination explicit while enabling further feature moves in smaller slices.

Impact:
- Main window now uses ViewLocator-based composition for toolbar, quick filters, and session health surfaces.
- Quick filters are functional and participate in the same rebuild pipeline as existing filters.
- Test suite increased to `84` passing tests.

Follow-ups:
- Move remaining inlined sections (filters, source tree, receiver setup, stream, details) into separate view files to complete the skeleton layout migration.

## 2026-03-28 - MVVM Refactor Phase 5B: Source Tree + Receiver Setup View Extraction
What changed:
- Added dedicated views for two previously inlined sections:
  [SourceTreeView.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/SourceTreeView.axaml),
  [ReceiverSetupView.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/ReceiverSetupView.axaml)
- Added minimal code-behind stubs:
  [SourceTreeView.axaml.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/SourceTreeView.axaml.cs),
  [ReceiverSetupView.axaml.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/ReceiverSetupView.axaml.cs)
- Updated shell view composition:
  [MainWindow.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml)
  - replaced inline source tree markup with `ContentControl` bound to `Sources`
  - replaced inline receiver setup editor markup with `ContentControl` bound to `ReceiverSetup` inside the existing expander

Why:
- These sections were still large inline fragments in `MainWindow.axaml`, which made shell layout edits noisy and harder to review.
- Extracting them keeps behavior unchanged while moving toward the intended panel-based view composition model.

Impact:
- No behavior change in source tree or receiver setup workflows.
- `MainWindow.axaml` is smaller and now delegates two more surfaces to ViewLocator.
- Full suite remains green (`84` passing tests).

Follow-ups:
- Extract remaining inlined surfaces (`LogFilters`, `LogStream`, `EntryDetails`) with care around `MainWindow` keyboard/auto-scroll behavior currently tied to named controls.

## 2026-03-28 - MVVM Refactor Phase 6A: Runtime Constructor Hardening (No Fallback Service Creation)
What changed:
- Tightened `MainWindowViewModel` constructor dependency contract:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
  - runtime constructor now requires explicit `IThemeService`, `IIngestionSession`, `IClipboardService`, `ISettingsStore`, and `ILogStatisticsService`
  - removed optional parameters and fallback `new ...` service creation from runtime path
  - kept a parameterless constructor only for design-time, explicitly marked as design-only
- Added a shared test factory for explicit VM construction:
  [MainWindowTestSupport.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowTestSupport.cs)
  - centralizes test construction with explicit dependencies
- Updated MainWindow-oriented tests to use the new support factory:
  [MainWindowFilteringTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowFilteringTests.cs),
  [MainWindowSessionAndDetailsTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowSessionAndDetailsTests.cs),
  [MainWindowReceiverSetupTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowReceiverSetupTests.cs),
  [MainWindowWorkspaceStateTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowWorkspaceStateTests.cs)

Why:
- Runtime composition should be container-owned and explicit, not a mix of DI plus hidden service fallbacks inside view-model constructors.
- This makes dependency ownership clearer and prevents accidental lifetime drift.

Impact:
- Runtime construction now fails fast if required dependencies are missing.
- Test setup remains straightforward through one helper while preserving explicit dependency flow.
- Full suite remains green (`84` passing tests).

Follow-ups:
- Move feature ViewModel composition further into DI registration so `MainWindowViewModel` stops constructing child VMs directly.

## 2026-03-28 - MVVM Refactor Phase 6B: Feature-Level DI Composition for Shell VM
What changed:
- Expanded application DI registrations for feature-level composition:
  [Root.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Composition/Root.cs)
  - added `ILogQueryEvaluator` registration
  - added child ViewModel registrations (`SourceTreeViewModel`, `QuickFiltersViewModel`, `ReceiverSetupViewModel`, `LogFiltersViewModel`, `LogStreamViewModel`, `EntryDetailsViewModel`, `SessionHealthViewModel`)
- Updated shell VM constructor to receive child ViewModels and query evaluator through DI:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
  - removed child-VM `new` construction from runtime path
  - assigns and wires injected child VMs
  - design-time constructor remains explicit and local to design composition only
- Updated test VM factory to mirror the new explicit constructor shape:
  [MainWindowTestSupport.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowTestSupport.cs)

Why:
- Shell composition should be container-owned at feature/service boundaries, not manually instantiated inside runtime view-model constructors.
- This keeps dependency ownership explicit while avoiding over-DI of row/item projection objects.

Impact:
- Runtime `MainWindowViewModel` composition now flows through DI for feature-level children.
- Query evaluation dependency is explicit and replaceable via DI.
- Full suite remains green (`84` passing tests).

Follow-ups:
- `MainToolbarViewModel` still depends on shell by design; if we want it fully DI-managed later, we should extract a toolbar coordination abstraction to break that dependency cycle.

## 2026-03-28 - MVVM Refactor Phase 7A: Log Stream View Extraction + Interaction Relocation
What changed:
- Extracted the log stream header/list/row template into a dedicated view:
  [LogStreamView.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/LogStreamView.axaml),
  [LogStreamView.axaml.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/LogStreamView.axaml.cs)
- Updated shell layout to compose stream surface via ViewLocator:
  [MainWindow.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml)
  - removed inline stream table/list markup
  - replaced with `ContentControl` bound to `Stream`
- Moved stream-specific UI behavior out of shell code-behind:
  [MainWindow.axaml.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml.cs)
  - removed list auto-scroll and list-scrolling selection logic
  - retained shell-level keyboard handling for global shortcuts, now delegating row movement through stream commands
- Extended stream VM for extracted-view interactions:
  [LogStreamViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/LogStreamViewModel.cs)
  - added `IsAutoScrollEnabled` state mirror
  - added `SelectNextEntry` / `SelectPreviousEntry` commands
- Synced shell auto-scroll state into stream VM:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
- Added selection-command coverage:
  [LogStreamViewModelTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/LogStreamViewModelTests.cs)

Why:
- `MainWindow.axaml` still carried the heaviest markup chunk (stream table + row template), which made shell decomposition stall.
- Stream behavior should be owned by the stream surface/viewmodel, not by window-level code-behind.

Impact:
- Shell markup and code-behind are both smaller and more composition-focused.
- Stream area is now independently evolvable (row template/layout/interaction changes no longer require shell edits).
- Full suite remains green (`85` passing tests).

Follow-ups:
- Extract details panel into its own view (`LogEntryDetailsView`) and then split query/filter surfaces into dedicated views to continue shell simplification.

## 2026-03-28 - MVVM Refactor Phase 7B: Entry Details View Extraction
What changed:
- Extracted the inline details panel into a dedicated view:
  [EntryDetailsView.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/EntryDetailsView.axaml),
  [EntryDetailsView.axaml.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/EntryDetailsView.axaml.cs)
- Updated shell layout composition:
  [MainWindow.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml)
  - replaced inline details markup with `ContentControl` bound to `Details`

Why:
- The details panel was still a large inline section in shell markup and made `MainWindow` harder to evolve.
- `EntryDetailsViewModel` already existed, so this was a low-risk extraction with no logic rewrite.

Impact:
- Shell markup is smaller and more region-oriented.
- Details UI is now independently editable without touching shell composition.
- Full suite remains green (`85` passing tests).

Follow-ups:
- Extract query/filter strip into dedicated views (`LogQueryBarView` + advanced filters) to complete shell-first decomposition.

## 2026-03-28 - MVVM Refactor Phase 7C: Filter Surface Extraction (Query Bar + Advanced Filters)
What changed:
- Added dedicated query-bar view:
  [LogQueryBarView.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/LogQueryBarView.axaml),
  [LogQueryBarView.axaml.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/LogQueryBarView.axaml.cs)
  - includes search, clear, sample generation, and column-width controls
  - exposes `FocusSearchBox()` for shell keyboard shortcut routing (`Ctrl+F`)
- Added dedicated advanced-filters view:
  [AdvancedLogFiltersView.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/AdvancedLogFiltersView.axaml),
  [AdvancedLogFiltersView.axaml.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/AdvancedLogFiltersView.axaml.cs)
  - contains structured fields, level toggles, and receiver setup expander composition
- Updated shell composition:
  [MainWindow.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml)
  - replaced large inline filter markup with `LogQueryBarView` + `AdvancedLogFiltersView`
- Updated shell keyboard handler to target extracted query bar:
  [MainWindow.axaml.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml.cs)

Why:
- The filter region remained one of the largest inline blocks in shell markup and slowed down iterative UI changes.
- Separating query bar from advanced filters matches intended UX structure without changing binding behavior.

Impact:
- `MainWindow.axaml` now focuses more on region placement and less on control-level implementation details.
- Search focus shortcut behavior is preserved after extraction.
- Full suite remains green (`85` passing tests).

Follow-ups:
- Extract a dedicated sidebar composition view to finalize shell-level markup reduction and prepare receiver-setup relocation from the filter band.

## 2026-03-29 - MVVM Refactor Phase 7D: Sidebar Composition + Receiver Setup Relocation
What changed:
- Added sidebar composition view model:
  [WorkspaceSidebarViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/WorkspaceSidebarViewModel.cs)
  - composes `Sources`, `QuickFilters`, and `ReceiverSetup` for a left-rail surface
- Added dedicated sidebar view:
  [WorkspaceSidebarView.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/WorkspaceSidebarView.axaml),
  [WorkspaceSidebarView.axaml.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/WorkspaceSidebarView.axaml.cs)
  - includes source tree, quick filters, and receiver setup expander in one panel
- Updated shell VM composition:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
  - exposes `WorkspaceSidebar` composed from existing child VMs
- Updated shell layout:
  [MainWindow.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml)
  - replaced left-column inline composition with `ContentControl` bound to `WorkspaceSidebar`
- Removed receiver setup expander from advanced filter surface:
  [AdvancedLogFiltersView.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/AdvancedLogFiltersView.axaml)

Why:
- Receiver setup was still mixed into the filter band, which diluted filter intent and increased top-surface complexity.
- Sidebar composition is a clearer structural unit and matches the intended `Sidebar | Stream | Inspector` workspace model.

Impact:
- Receiver setup is now located in the left rail with related source/navigation surfaces.
- Filter surface is smaller and focused on query concerns.
- Full suite remains green (`85` passing tests).

Follow-ups:
- Move session health to a true bottom status row and convert center area to explicit three-region workspace proportions for final shell layout polish.

## 2026-03-29 - MVVM Refactor Phase 7E: Three-Region Workspace Layout + Bottom Status Dock
What changed:
- Reworked shell workspace layout in:
  [MainWindow.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/MainWindow.axaml)
  - changed center workspace to explicit columns: `Sidebar | Stream | Inspector` (`260,*,360`)
  - bound `Stream` to center column and `Details` to right inspector column
  - moved `SessionHealth` out of center content and docked it in a dedicated bottom row

Why:
- The previous center area still stacked details and status beneath the stream, reducing stream prominence and mixing app-state with content.
- A clear three-region workspace with bottom-docked session status is closer to operator-focused log viewer ergonomics.

Impact:
- Log stream now remains the visual center while details are consistently presented as a right inspector.
- Session health is now presented as app-state at the bottom of the shell.
- Full suite remains green (`85` passing tests).

Follow-ups:
- If needed, replace `SessionHealthView` with a thinner horizontal status-bar variant for denser bottom-dock presentation.

## 2026-03-29 - MVVM Refactor Phase 8A: Sidebar Composition via DI
What changed:
- Moved sidebar composition ownership from shell runtime code into DI:
  [Root.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Composition/Root.cs)
  - registered `WorkspaceSidebarViewModel` as a singleton feature VM
- Updated shell VM constructor contract:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
  - added `WorkspaceSidebarViewModel` as an explicit dependency
  - removed manual runtime `new WorkspaceSidebarViewModel(...)` composition
- Updated test support construction to mirror the new shell constructor shape:
  [MainWindowTestSupport.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowTestSupport.cs)

Why:
- Feature-level shell composition should be container-owned at runtime to keep dependency ownership explicit.
- Manual shell-side instantiation of sidebar composition was the remaining mixed-construction hotspot.

Impact:
- Runtime shell composition no longer constructs sidebar VM manually.
- `MainWindowViewModel` dependencies are more explicit and aligned with the existing DI approach for other feature VMs.
- Full suite remains green (`85` passing tests).

Follow-ups:
- Convert `SessionHealthView` from card-style panel to a denser horizontal bottom status bar surface.

## 2026-03-29 - MVVM Refactor Phase 8B: Horizontal Bottom Status Bar Surface
What changed:
- Reworked bottom health view presentation in:
  [SessionHealthView.axaml](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Views/Status/SessionHealthView.axaml)
  - replaced stacked card layout with a single-row horizontal status surface
  - kept existing `SessionHealthViewModel` bindings unchanged (`ActiveReceiversText`, `BufferedEntriesText`, `StructuredEventsText`, `DroppedPacketsText`)
  - switched background from `ShellBackgroundBrush` to `ShellPanelBrush` for clearer shell status bar contrast

Why:
- The previous vertical card shape consumed too much height for a bottom-docked shell status region.
- A horizontal status row better matches operator workflows where log stream density is prioritized.

Impact:
- No behavior or data-flow changes; presentation-only refactor.
- Bottom status area is denser and visually consistent with the shell’s docked status intent.
- Full suite remains green (`85` passing tests).

Follow-ups:
- If we need stronger scan-ability under load, split each status metric into a compact label/value token style.

## 2026-03-29 - MVVM Refactor Phase 8C: MainWindowViewModel Convention Cleanup
What changed:
- Removed record-based design-time dependency packaging from:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
  - deleted `DesignDependencies` record
  - deleted `CreateDesignDependencies()` helper
  - restored a minimal direct design-time constructor path

Why:
- The extra design-time record/helper added more structure to `MainWindowViewModel` than needed and pushed against the shell-thinning direction.
- Keep shell VM changes focused on runtime DI composition and avoid adding new local scaffolding patterns.

Impact:
- No runtime behavior change.
- Main shell VM remains simpler and aligns better with existing project conventions.
- Full suite remains green (`85` passing tests).

Follow-ups:
- Continue reducing `MainWindowViewModel` responsibility by extracting workspace/state coordination logic into dedicated services in small phases.

## 2026-03-29 - MVVM Refactor Phase 9A: Workspace State Coordination Service Extraction
What changed:
- Added dedicated workspace state coordination service:
  [IWorkspaceStateCoordinator.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/IWorkspaceStateCoordinator.cs),
  [WorkspaceStateCoordinator.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/WorkspaceStateCoordinator.cs),
  [WorkspaceStateSnapshot.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/WorkspaceStateSnapshot.cs),
  [WorkspaceStateUpdate.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/WorkspaceStateUpdate.cs)
- Registered coordinator in DI:
  [Root.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Composition/Root.cs)
- Simplified shell VM workspace persistence responsibilities:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
  - removed direct `ISettingsStore` workspace load/save usage from shell VM
  - removed shell-owned debounce/cancellation save plumbing
  - moved save queue to coordinator via `WorkspaceStateUpdate`
- Updated test support constructor wiring:
  [MainWindowTestSupport.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowTestSupport.cs)

Why:
- Workspace load/debounce-save policy is application coordination logic, not shell presentation state.
- This extraction reduces `MainWindowViewModel` ownership scope while preserving existing behavior.

Impact:
- Shell VM now maps UI state to coordinator input instead of owning settings persistence workflow.
- Workspace and UI layout persistence behavior remains debounced and asynchronous.
- Full suite remains green (`85` passing tests).

Follow-ups:
- Extract status text/statistics/session summary formatting from `MainWindowViewModel` into a dedicated shell status presenter.

## 2026-03-29 - MVVM Refactor Phase 9B: Shell Status Formatting Extraction
What changed:
- Added dedicated shell status formatting service and output model:
  [IShellStatusFormatter.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/IShellStatusFormatter.cs),
  [ShellStatusFormatter.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/ShellStatusFormatter.cs),
  [ShellStatusPresentation.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/ShellStatusPresentation.cs)
- Registered formatter in DI:
  [Root.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Composition/Root.cs)
- Simplified shell VM status responsibilities:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
  - removed local status/statistics/session summary formatting helpers
  - replaced them with one `UpdateShellStatusPresentation()` call that maps formatter output into shell and `SessionHealth` view model state
- Updated test construction wiring:
  [MainWindowTestSupport.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowTestSupport.cs)
- Added focused formatter unit tests:
  [ShellStatusFormatterTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/ShellStatusFormatterTests.cs)

Why:
- Text shaping and summary formatting are presentation policy and were bloating `MainWindowViewModel`.
- Centralizing this behavior in one service keeps shell VM focused on flow/orchestration.

Impact:
- No behavior change in displayed shell status information.
- `MainWindowViewModel` now delegates status formatting instead of implementing it directly.
- Full suite remains green (`87` passing tests).

Follow-ups:
- Extract log stream refresh orchestration (`OnEntriesAppended` + rebuild trigger flow) into a dedicated shell coordinator service.

## 2026-03-29 - MVVM Refactor Phase 9C: Log Stream Projection Service Extraction
What changed:
- Added dedicated log-stream projection service:
  [ILogStreamProjectionService.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/ILogStreamProjectionService.cs),
  [LogStreamProjectionService.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/LogStreamProjectionService.cs),
  [QuickFilterSnapshot.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/QuickFilterSnapshot.cs)
- Registered projection service in DI:
  [Root.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Composition/Root.cs)
- Simplified stream/filter orchestration in shell VM:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
  - moved append/rebuild/filter-match logic into projection service
  - moved quick-filter snapshot computation into projection service
  - removed direct query-evaluator dependency from shell VM
- Updated test construction wiring:
  [MainWindowTestSupport.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowTestSupport.cs)
- Added dedicated projection service tests:
  [LogStreamProjectionServiceTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/LogStreamProjectionServiceTests.cs)

Why:
- Stream projection/filtering logic is orchestration policy and was still heavily embedded in `MainWindowViewModel`.
- Extracting this logic keeps the shell VM focused on event wiring and state flow rather than filter internals.

Impact:
- No behavior change in log visibility/filtering flow.
- `MainWindowViewModel` is leaner and delegates stream projection mechanics to an app service.
- Full suite remains green (`90` passing tests).

Follow-ups:
- Continue shell thinning by extracting receiver/workspace load orchestration sequencing from `MainWindowViewModel`.

## 2026-03-29 - MVVM Refactor Phase 9D: Sample Entry Generation Service Extraction
What changed:
- Added dedicated sample log-entry generation service:
  [ISampleLogEntryGenerator.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/ISampleLogEntryGenerator.cs),
  [SampleLogEntryGenerator.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Services/SampleLogEntryGenerator.cs)
- Registered generator in DI:
  [Root.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/Composition/Root.cs)
- Updated shell VM to delegate sample creation:
  [MainWindowViewModel.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Application/ViewModels/MainWindowViewModel.cs)
  - removed in-VM random/sample construction logic
  - removed `PickRandomLevel()` helper from shell VM
- Updated test support wiring:
  [MainWindowTestSupport.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/MainWindowTestSupport.cs)
- Added generator unit tests:
  [SampleLogEntryGeneratorTests.cs](/C:/Workspace/SamLabs.Beobachter/SamLabs.Beobachter.Tests/Application/SampleLogEntryGeneratorTests.cs)

Why:
- Sample event generation is not shell coordination responsibility and should not live in `MainWindowViewModel`.
- Moving it to a service keeps shell VM focused on state/flow and improves testability.

Impact:
- No behavior change in sample-entry command output shape.
- `MainWindowViewModel` is smaller and has one less feature-specific concern.
- Full suite remains green (`92` passing tests).

Follow-ups:
- Continue splitting receiver/workspace load sequencing from shell VM into a dedicated startup orchestration service.
