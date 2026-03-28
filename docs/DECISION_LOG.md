# Decision Log

Purpose: keep stable architecture and implementation decisions in one place, with rationale and consequences.

## Entry Template

```md
## YYYY-MM-DD - Decision Title
Status:
- Accepted | Superseded | Rejected

Decision:
- ...

Why:
- ...

Consequences:
- ...
```

---

## 2026-03-28 - Log4j XML Schema Strategy
Status:
- Accepted

Decision:
- Treat legacy log4j/log4net-compatible XML (`log4j:event`) and modern log4j2 `XmlLayout` (`Event`) as two different payload shapes.
- Do not assume one schema is always current across all producers.
- Keep parsing tolerant by matching element local names and by accepting equivalent attribute names (`logger`/`loggerName`, `thread`/`threadName`, `timestamp`/`timeMillis`).

Why:
- Legacy Log2Console usage and many .NET senders still emit log4j1-style XML compatibility events.
- log4j2 uses a different XML shape and fields.
- Hard-coding one schema would silently drop valid events from mixed environments.

Consequences:
- Parser implementation must include explicit schema branching and tests for both shapes.
- Receiver/session code can stay schema-agnostic and depend only on normalized `LogEntry`.
