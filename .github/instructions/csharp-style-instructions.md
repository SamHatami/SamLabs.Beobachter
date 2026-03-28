# C# Coding Style — Agent Instructions

These rules are grounded in the [.NET Runtime coding style](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md)
and [Microsoft C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).
Apply them consistently when writing or modifying code.

---

## Access Modifiers

- Always write access modifiers explicitly, including `private`.
- The access modifier must be the **first** modifier (e.g. `public abstract`, not `abstract public`).
- Avoid `this.` unless strictly necessary to disambiguate.

```csharp
// Correct
private readonly int _count;

// Wrong
readonly int _count;
```

---

## Type Member Ordering

Organize members in this top-to-bottom order:

1. Constants (`const`)
2. Static fields
3. Instance fields
4. Properties
5. Constructors
6. Public methods
7. Protected methods
8. Private methods
9. Nested types

---

## Ordering Within Fields

Within the fields section, order by:

1. Accessibility (public → internal → protected → private)
2. Static before instance (within each accessibility group)
3. Readonly before mutable (within each static/instance group)

Note: `static` always comes before `readonly` — write `static readonly`, not `readonly static`.

```csharp
public static readonly int MaxCount = 100;
public static int ActiveCount;
public readonly string Name;
public string Label;

private static readonly ILogger s_logger = ...;
private static int s_instanceCount;
private readonly int _id;
private int _value;
```

---

## Ordering Within Properties

Same principle as fields:

1. Accessibility (public → internal → protected → private)
2. Static before instance
3. Read-only (`get`-only) before read-write

```csharp
public static string Version { get; }
public static string BuildTag { get; set; }
public int Id { get; }
public string Name { get; set; }

private bool IsInitialized { get; }
private int CachedValue { get; set; }
```

---

## Constructors

- Place after all fields and properties.
- Order multiple constructors from fewest to most parameters.
- Chain constructors with `this(...)` where appropriate rather than duplicating logic.

---

## Methods

- Order by accessibility: public → protected → private.
- Within the same accessibility, group by logical concern rather than alphabetically.
- Place `override` methods near the top of their accessibility group.

---

## Naming

| Member type | Convention | Example |
|---|---|---|
| Private / internal instance field | `_camelCase` | `_nodeCount` |
| Private / internal static field | `s_camelCase` | `s_instanceCount` |
| Private / internal thread-static field | `t_camelCase` | `t_threadId` |
| Public field | `PascalCase` | `MaxIterations` |
| Property | `PascalCase` | `IsInitialized` |
| Method | `PascalCase` | `RunAnalysis()` |
| Local function | `PascalCase` | `ComputeOffset()` |
| Local variable | `camelCase` | `resultSet` |
| Parameter | `camelCase` | `frameModel` |
| Constant | `PascalCase` | `DefaultTolerance` |
| Interface | `IPascalCase` | `IAnalysisProvider` |
| Type parameter | `T` or `TPascalCase` | `TResult` |

---

## Braces and Formatting

- Use **Allman style**: each brace on its own new line, aligned to the current indentation level.
- Always use braces for `if`/`else`/`for`/`while` blocks **if any branch uses braces**, or if the body spans multiple lines.
- Braces may be omitted only if **every** branch of the compound statement fits on a single line.
- Never use single-line form (e.g. `if (x == null) throw ...` on one line).
- Four spaces of indentation — no tabs.
- One blank line between member declarations. No more than one consecutive blank line anywhere.

```csharp
// Correct — Allman braces
if (value > 0)
{
    Process(value);
}

// Wrong — K&R style
if (value > 0) {
    Process(value);
}

// Wrong — single-line form
if (value > 0) Process(value);
```

---

## `var` Usage

- Use `var` **only** when the type is explicitly visible on the right-hand side — typically via `new` or an explicit cast.
- Do not use `var` when the type must be inferred from a method name or return type.

```csharp
// Correct — type is explicit on the right
var stream = new FileStream(...);
var result = (AnalysisResult)rawResult;

// Wrong — type is not obvious
var result = GetAnalysisResult();
var count = items.Count();
```

Target-typed `new()` is allowed when the type is explicitly named on the left-hand side:

```csharp
FileStream stream = new(...); // OK
```

---

## Language Keywords vs. BCL Types

- Always use language keywords over BCL type names, for both type references and method calls.

```csharp
// Correct
int count = int.Parse(input);
string name = "value";

// Wrong
Int32 count = Int32.Parse(input);
String name = "value";
```

---

## Namespaces and `using` Directives

- Place `using` directives **outside** the namespace declaration, at the top of the file.
- Sort alphabetically, but put `System.*` namespaces above all others.
- Prefer **file-scoped namespace declarations** for files containing a single namespace.

```csharp
using System;
using System.Collections.Generic;
using MyApp.Core;

namespace MyApp.Analysis;

public class AnalysisManager { ... }
```

---

## Miscellaneous

- Use `nameof()` instead of string literals when referring to member names.
- Prefer `is null` / `is not null` over `== null` / `!= null`.
- Use `&&` and `||` (short-circuit) rather than `&` and `|` in boolean expressions.
- Use expression-bodied members (`=>`) only for trivial single-expression getters or methods.
- Use string interpolation (`$"..."`) for short concatenations; `StringBuilder` for loops.
- Use XML doc comments (`/// <summary>`) on all public members.
- Place comments on their own line, not at the end of a line of code.
- Internal and private types should be `static` or `sealed` unless derivation is required.
