# SamLabs.Beobachter Rebuild Plan

## Scope
Rebuild the old Log2Console fork as a modern Avalonia + MVVM desktop app with:
- dark/light theme support
- clean project boundaries (`App -> Infrastructure -> Core`)
- reusable receiver/parsing concepts from the legacy code, without carrying legacy WinForms or obsolete platform baggage

## Legacy Sweep Summary

### `Old_forked_source/docs`
- `Home.md`, `ClientConfiguration.md`, `NLog.md`: still useful as feature and client-config reference.
- `Screenshots.md` + images: useful only as UX reference for parity checks.
- Most docs are historical and should not be copied as product docs without rewriting.

### `Old_forked_source/src` high-value findings

Reusable ideas (rewrite, do not copy 1:1):
- Log domain model shape: `LogMessage`, `LogLevel`, structured properties.
- Log level mapping/ranges from multiple sources — see note below on integer-based level normalization.
- Receiver abstraction and pluggable receiver concept.
- Parsers in `ReceiverUtils` for log4j/NLog XML payloads — the shared `XmlReaderSettings` and `XmlParserContext` instances are a deliberate performance optimization worth preserving in the rewrite.
- File tailing and CSV ingestion concepts.
- Core UX behaviors from `MainForm`:
  - batching incoming messages via queue + timer
  - level filtering, text search, pause, auto-scroll
  - logger tree grouping/enable-disable
  - message details, exception view, source-file mapping
  - export selected logs

Do not carry over:
- WinForms UI and designer code (`MainForm*`, `SettingsForm*`, `ReceiversForm*`, `AboutForm*`, `UI/*` WinForms widgets).
- Vendored UI/framework code:
  - `ICSharpCode.TextEditor/*`
  - `RichTextBoxLinks/*`
  - `External/WindowsAPICodePack/*`
  - `Win32ApiCodePack/*`
- Legacy transports/platform-specific receivers:
  - `.NET Remoting`
  - `Silverlight Socket Policy`
  - `MSMQ` (unless explicitly required)
  - `WinDebug` and `EventLog` as first-pass cross-platform blockers
- `BinaryFormatter`-based settings persistence.
- Legacy setup project (`L2C_Setup.vdproj`) and test harness projects (`Test`, `TestNLog`) as product code.

## Proposed Target Architecture

Create projects defined in `AGENTS.md`:
- `SamLabs.Beobachter.Core`
  - Models: `LogEntry`, `LogLevel`, `LogProperty`
  - `LoggerNode` trie — see note below
  - Log level normalization table — see note below
  - Filters/query contracts
  - Receiver/parsing contracts (`ILogReceiver`, `ILogParser`)
  - Session/log store contracts
- `SamLabs.Beobachter.Infrastructure`
  - Receivers: `FileTailReceiver`, `UdpReceiver`, `TcpReceiver`, `CsvFileReceiver`
  - Parsers: `Log4jXmlParser`, `CsvParser`, optional plain-text parser
  - Settings persistence (JSON via `System.Text.Json` with source generation, in `%LOCALAPPDATA%/SamLabs.Beobachter/`)
- `SamLabs.Beobachter.App` (rename from current `SamLabs.Beobachter.Application`)
  - Avalonia views/viewmodels
  - themes/resources
  - composition root + DI
- `SamLabs.Beobachter.Tests`
  - Core-first tests, then deterministic infrastructure tests

## Key Domain Design Notes

### Log level normalization (integer-based)
log4j and NLog report levels as integers, not strings. The legacy `LogLevels` class maps integer ranges to named levels using `RangeMin`/`RangeMax` buckets — for example, any value between 10001 and 30000 maps to `Debug`. This range table must be reproduced explicitly in `Core` as part of `LogLevelTable` or similar, not reconstructed ad hoc in parsers. The string-based path (`"DEBUG"`, `"WARN"`, etc.) must also be supported for sources that send text levels. Both lookup paths should be covered by unit tests.

### Logger tree (trie over dot-separated names)
The legacy `LoggerItem.GetOrCreateLogger` method implements a trie over dot-separated logger name segments (e.g. `"MyApp.Services.Auth"` decomposes into three levels). This structure is the backbone of the logger panel. In the new architecture, this trie — called `LoggerNode` — belongs in `Core` as a pure data structure with no UI or infrastructure dependencies. `LoggerNode` should support: path-based get-or-create, enable/disable with optional recursive propagation, and enumeration of all nodes. ViewModels in `App` bind to it and translate it into Avalonia tree items; they do not reimplement path decomposition themselves.

### Receiver integration shape
The legacy `ILogMessageNotifiable` push-callback pattern is replaced by a `Channel<LogEntry>`-based pipeline. Each `ILogReceiver` implementation in `Infrastructure` receives an injected `ChannelWriter<LogEntry>` at start and writes parsed entries to it. The session layer in `App` or an optional `Application` project owns the `Channel<LogEntry>`, reads from the `ChannelReader<LogEntry>` end, and dispatches batched updates to ViewModels. This makes the data flow explicit and testable: receivers only write, consumers only read, and no receiver ever touches the UI thread.

### Channel backpressure policy
Use a bounded channel for ingest (default capacity `50_000`, configurable via settings). Set `BoundedChannelFullMode.DropOldest` so the app remains responsive under sustained spikes and preserves the most recent logs. Track a dropped-message counter in the session layer and surface it in diagnostics/UI so data loss is visible. This policy is intentional: for an interactive viewer, recency and responsiveness take priority over unbounded memory growth.

### Settings persistence
The legacy `UserSettings` class is a single serializable monolith using `BinaryFormatter`. The replacement uses `System.Text.Json` with source-generated serialization contexts for performance and AOT compatibility. Settings are split into focused, independently serializable records: `AppSettings`, `ReceiverDefinitions`, `WorkspaceSettings`, and `UiLayoutSettings`. Each has its own JSON file under `%LOCALAPPDATA%/SamLabs.Beobachter/`. New settings are added through typed records, never through scattered string keys. A migration shim is added only if old `BinaryFormatter` data needs to be read once and converted.

## Feature Priorities

### P0 (MVP parity)
- Receive logs from file, UDP, TCP.
- Parse log4j/NLog XML payloads (including integer-level normalization) + basic plain text.
- Display log list with virtualized UI and batching.
- Level filter + text search.
- Logger grouping/tree and enable-disable.
- Details panel (message/properties/exception/source fields).
- Dark/light theme toggle plus system-follow mode.
- JSON settings persistence.

### P1 (high-value improvements)
- Better query/filter model (multi-criteria filters).
- Workspace/session save and restore.
- Source file navigation mapping.
- Export selected/all logs to CSV/JSON.
- Throughput and memory instrumentation.

### P2 (optional/legacy-dependent)
- WebSocket receiver (if needed).
- Windows-only receivers behind capability flags (`EventLog`, `WinDebug`).
- Additional import/export formats.

## Implementation Phases

1. Foundation
- Restructure solution into `Core`, `Infrastructure`, `App`, `Tests`.
- Add DI composition root and baseline app shell.
- Add theme dictionaries (`Light.axaml`, `Dark.axaml`) and runtime switch service.

2. Core domain
- Implement immutable `LogEntry` and `LogProperty` models.
- Implement `LogLevelTable` with both integer-range and string lookup paths, and tests for both.
- Implement `LoggerNode` trie with path decomposition, recursive enable/disable, and enumeration.
- Define `ILogReceiver` with `ChannelWriter<LogEntry>`-based integration shape and explicit lifecycle (`Start`, `Stop`, `Dispose`).
- Define channel options and backpressure policy (`Bounded`, `DropOldest`, dropped-count telemetry contract).
- Define `ILogParser` contract.
- Implement in-memory log store API with append/batch semantics.
- Define filter/query contracts.

3. Infrastructure ingestion
- Port and modernize parsers from `ReceiverUtils` into `Infrastructure.Parsing`. Preserve shared `XmlReaderSettings`/`XmlParserContext` for performance. Both integer-level and string-level inputs must be handled.
- Implement `FileTailReceiver`, `UdpReceiver`, `TcpReceiver`, `CsvFileReceiver`, each writing to an injected `ChannelWriter<LogEntry>` with cancellation-safe async loops.
- Implement split JSON settings persistence (`AppSettings`, `ReceiverDefinitions`, `WorkspaceSettings`, `UiLayoutSettings`) using `System.Text.Json` source generation.

4. App shell and MVVM surfaces
- Main workspace layout: receiver panel, log list, filters, details pane.
- `LoggerTreeViewModel` binds to `LoggerNode` trie from Core; does not reimplement path decomposition.
- ViewModels with CommunityToolkit source generators.
- Session layer owns `Channel<LogEntry>`, reads from reader end, dispatches batched updates to ViewModels.
- Bind to log store snapshots with batched UI updates.

5. Feature parity pass
- Pause/resume ingest.
- Auto-scroll control.
- Logger tree behaviors (grouping, enable-disable, clear/collapse) driven by `LoggerNode`.
- Exception/source details and clipboard copy.

6. Settings and persistence
- Wire typed settings records into DI.
- Persist receiver definitions, UI layout, columns, theme, filters.
- Add migration shim for old `BinaryFormatter` values only if required.

7. Test and hardening
- Unit tests: `LogLevelTable` (integer ranges and string lookup), `LoggerNode` (path decomposition, recursive enable/disable), parsing, filtering, log store behavior.
- Infra tests: file tailing, UDP/TCP receive loops, cancellation/disposal, settings serialization round-trips.
- Load tests for high-volume ingest and UI responsiveness.

8. Cleanup and docs
- Remove obsolete legacy code from active path.
- Rewrite README/docs for new architecture and receiver setup.
- Keep `Old_forked_source` as historical reference only.

## Immediate Next Steps
1. Create `Core`, `Infrastructure`, and `Tests` projects and wire references.
2. Implement `LogLevelTable` and `LoggerNode` in Core with tests.
3. Define `ILogReceiver` with `ChannelWriter<LogEntry>` integration shape.
4. Port `ReceiverUtils` parsing logic into `Infrastructure.Parsing` with tests covering both integer and string level inputs.
5. Build a minimal `MainWindow` showing batched logs and dark/light theme switching.

## Risks and Mitigations
- Risk: UI freezes under heavy log rates.
- Mitigation: enforce producer/consumer batching via `Channel<T>`; UI thread receives only pre-batched snapshots.

- Risk: feature creep from legacy receiver set.
- Mitigation: lock MVP receivers to File/UDP/TCP/CSV first.

- Risk: legacy behavior uncertainty.
- Mitigation: document parity decisions and add characterization tests around parser behavior, especially integer-level normalization edge cases.

- Risk: settings migration breaks on first run if old `BinaryFormatter` file exists.
- Mitigation: treat missing or unreadable settings file as a clean start; migration shim is opt-in, not on the critical path.

- Risk: hidden data loss during high ingest rates.
- Mitigation: explicit bounded channel policy (`DropOldest`) plus dropped-message counters and visible diagnostics.
