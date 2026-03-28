# AGENTS.md

## Purpose
This repository is an Avalonia application using MVVM with CommunityToolkit.Mvvm.

Your job is to make changes that are:
- correct
- small and reviewable
- architecturally consistent
- easy to test
- easy to extend without accumulating hidden coupling

Follow the nearest good existing pattern in the repo before introducing a new one.

---

## Core principles
1. Keep architecture boundaries clean.
2. Reuse before inventing.
3. Extract only when there is a real responsibility to extract.
4. Keep ViewModels focused on screen state and user intent.
5. Keep Views thin.
6. Keep infrastructure and platform APIs out of ViewModels.
7. Prefer consistency over cleverness.
8. Keep diffs coherent and easy to review.

---

## Required implementation workflow

For any non-trivial change, follow this order:

1. Inspect the relevant feature folder.
2. Inspect nearby related classes that solve similar problems.
3. Decide whether the new logic should be:
   - kept local
   - reused from an existing abstraction
   - extracted into a new abstraction
4. Implement using the closest good existing pattern.
5. Refactor only when duplication or responsibility drift is real.
6. Summarize the reuse/extraction decision in the final change note.

Do not skip the inspection step and immediately add new patterns.

---

## Architecture boundaries

Use this dependency direction:

Views -> ViewModels -> application/domain services -> infrastructure

Rules:
- Views must not contain business logic.
- ViewModels must not reference View types.
- ViewModels must not directly access platform APIs.
- Domain and application logic must not depend on Avalonia UI types.
- Infrastructure implements interfaces used by upper layers.
- Do not let persistence, dialogs, file access, clipboard access, or window management leak upward into ViewModels.

When choosing where code belongs, place it in the lowest layer that can own it cleanly.

---

## Folder and feature organization

Prefer feature-oriented organization when practical.

Good:
- `Features/Editor/EditorView.axaml`
- `Features/Editor/EditorViewModel.cs`
- `Features/Editor/IEditorSessionService.cs`

Avoid growing global folders full of unrelated ViewModels, services, and helpers unless the repo is still very small.

Keep each feature internally consistent.

---

## View rules

Views exist to define layout, bindings, styling, and view-only interaction.

Allowed in Views/code-behind:
- control initialization
- view-only behavior
- focus handling
- lifecycle glue
- visual state behavior that is strictly UI-specific
- platform/view-specific interaction that cannot reasonably be expressed through binding

Not allowed in Views/code-behind:
- business rules
- persistence logic
- feature orchestration
- application workflow policy
- cross-feature coordination

Keep code-behind minimal.

Use one consistent view-resolution pattern in a given area of the repo.
Do not mix competing patterns in the same feature.

Prefer compile-safe bindings.
Use explicit types for binding scopes where the repo pattern expects it.

Use resources intentionally:
- theme-sensitive values should stay theme-friendly
- structural resources should remain stable and predictable
- do not scatter one-off resources everywhere without need

---

## ViewModel responsibility contract

### What a ViewModel is for
A ViewModel exists to model screen state and user intent.

A ViewModel may:
- expose bindable state
- expose commands
- coordinate one screen-level user flow
- transform application/domain results into UI-friendly state
- manage loading, selection, filtering, status, and error presentation for the screen

A ViewModel is not:
- a business logic container
- a repository
- a serializer
- a platform API wrapper
- a dialog implementation
- a file system gateway
- a dumping ground for unrelated feature logic

### What belongs in a ViewModel
Typical ViewModel responsibilities:
- selected item state
- search/filter text
- tab or panel state
- command enablement
- busy/error/success state
- screen-level data shaping for display
- orchestration of service calls needed by a single UI flow

Examples:
- `SelectedItem`
- `SearchText`
- `IsBusy`
- `ErrorMessage`
- `LoadAsync()`
- `SaveCommand`

### What does not belong in a ViewModel
Move these out:
- rules that make sense without the screen
- reusable validation logic
- reusable parsing/formatting logic
- persistence and storage access
- HTTP/database/file I/O
- clipboard and dialog details
- cross-feature workflows
- direct references to Avalonia controls, windows, visual tree, or `TopLevel`

### The screen test
Before adding logic to a ViewModel, ask:

Would this logic still make sense if this screen did not exist?

- If yes, it probably belongs in a service, domain object, or dedicated helper.
- If no, it may belong in the ViewModel.

### The second-screen test
Ask:

If another screen needed this behavior, would I want to copy this code?

- If yes, extract it.
- If no, keep it local.

### ViewModel coordination rule
A ViewModel may coordinate a workflow:
1. gather UI state
2. call services
3. update bindable properties
4. expose result state

It should not own deep business logic behind that workflow.

### ViewModel size rule
A ViewModel should usually correspond to:
- one screen
- one dialog
- one clearly bounded panel
- one clearly bounded workflow surface

If it starts owning unrelated parts of the app, split it.

---

## Reuse and duplication rules

### Always inspect related code first
Before adding a new property, command, helper, service, or pattern:
- check the same feature folder
- check sibling features with similar behavior
- check related ViewModels and services
- look for existing approaches to naming, commands, async flows, validation, and error handling

Do not introduce a new pattern if a good one already exists nearby.

### Reuse before copying
If another class already solves the same problem:
- reuse it as-is when possible
- extend it if that keeps ownership clear
- extract only if duplication is real and the responsibility has a clear name

Do not solve repeated problems with copy-paste.

### Rule of use before extraction
- Used once: usually keep it local.
- Used twice in closely related places: consider extraction.
- Used three or more times: extraction is usually preferred.

Do not extract too early into vague abstractions.

### No dumping-ground helper classes
Avoid creating classes named:
- `Utils`
- `Helpers`
- `Common`
- `Shared`
- `Manager`

Choose names that describe a real responsibility, such as:
- `DocumentSelectionService`
- `ProjectValidator`
- `FileNameFormatter`
- `RecentFilesStore`

If you cannot name the responsibility clearly, the abstraction is probably not ready.

### Extract by responsibility
When shared logic appears, put it where it naturally belongs:

- screen coordination -> feature service or ViewModel-adjacent coordinator
- business rule -> domain/application service
- formatting/parsing -> dedicated formatter/parser
- validation -> validator or rule service
- platform interaction -> infrastructure service behind an interface

Do not move mixed responsibilities into a shared class just to reduce duplication.

### When a helper grows
If a helper starts getting large:
1. check whether it now has more than one responsibility
2. split it by capability or domain concept
3. rename it to reflect its real purpose
4. move it into the proper layer if it drifted upward or downward

A growing helper often means a real service or domain concept is missing.

---

## Services and abstractions

Use services to hold logic that should not live in Views or ViewModels.

Good service candidates:
- reusable feature workflows
- validation used by multiple callers
- persistence coordination
- application use cases
- formatting/parsing reused across features
- infrastructure access
- platform-specific capabilities

Keep service boundaries intentional.

Do not create abstractions with no clear ownership or purpose.

Prefer interface-based boundaries when they improve:
- testability
- separation from infrastructure
- replacement of platform-specific behavior
- reuse across multiple callers

Do not create interfaces only out of habit.

---

## Platform and infrastructure rules

Anything that depends on platform/UI host services should be abstracted behind a service when used from ViewModels.

Examples:
- file picking
- folder picking
- clipboard
- launching files or URIs
- notifications
- dialogs
- window coordination

ViewModels should depend on abstractions, not direct platform access.

Infrastructure code should:
- implement interfaces
- stay outside ViewModels
- avoid leaking UI framework details into domain/application logic

---

## CommunityToolkit.Mvvm rules

Prefer toolkit-generated MVVM code over handwritten boilerplate.

Use:
- observable properties for bindable state
- relay commands for user actions
- async command patterns for async work
- observable base types or equivalent repo-standard base classes

Generator-based ViewModels must use the correct class shape for source generation.
Do not manually write repetitive property-notification code unless there is a clear reason.

Prefer command-driven interaction over event-driven application logic.

Do not put long-running or side-effect-heavy work inside property setters.

---

## Dependency injection rules

Use constructor injection for:
- ViewModels
- services
- repositories
- platform abstractions

Do not use service location from arbitrary classes.

Register dependencies in a centralized composition root.
Do not scatter service registration logic across unrelated files.

Choose lifetimes intentionally.
Do not default everything to singleton without thinking through state ownership.

---

## Async, state, and error handling

Use async/await for I/O and long-running work.

Do not block async work with `.Result`, `.Wait()`, or equivalent patterns.

Expose screen state explicitly when useful:
- `IsBusy`
- `IsLoading`
- `StatusMessage`
- `ErrorMessage`
- progress state
- empty state

Do not hide failure paths.
Do not swallow exceptions silently.
Do not leave ViewModels in unclear intermediate state after failure.

Keep cancellation and reentrancy in mind for user-triggered async operations.

---

## Naming and code shape

Prefer explicit names that reveal intent.

Good:
- `LoadProjectAsync`
- `CanSave`
- `SelectedDocument`
- `ApplyFilter`
- `ProjectValidator`

Bad:
- `Handle`
- `Process`
- `Data`
- `Stuff`
- `Manager` unless it really coordinates a domain-relevant responsibility

Keep methods small and focused.
If a command method becomes large, extract helpers or move deeper logic into a service.

Prefer composition over inheritance.
Avoid deep ViewModel inheritance trees.

---

## Testing expectations

ViewModels should be testable without launching the UI.

Prefer tests for:
- state transitions
- command enablement
- async success/failure behavior
- validation behavior
- extraction-worthy shared services
- bug fixes with meaningful logic

Mock boundaries, not private implementation details.

Do not make tests brittle by coupling them to internal structure unnecessarily.

---

## Smells that require refactoring attention

Refactor when you see these signs:
- very large ViewModels
- too many injected dependencies
- repeated logic across multiple ViewModels
- direct infrastructure calls from ViewModels
- helper classes accumulating unrelated methods
- code-behind growing beyond view-only behavior
- business rules mixed with presentation state
- multiple competing patterns in one feature
- hard-to-test command methods
- comments needed to explain ownership confusion

A large constructor is a warning sign, not something to ignore.

Do not solve these smells by introducing a vague god-service.

---

## Change discipline rules

Before changing architecture in an area:
- understand the local pattern
- preserve working consistency unless the pattern is clearly harmful
- do not partially migrate a feature into a conflicting style
- do not reshape folders or abstractions without a meaningful reason

Prefer local consistency over theoretical perfection.

Only introduce a new abstraction when it clearly improves:
- ownership
- reuse
- testability
- readability
- separation of concerns

Keep changes proportional to the task.

---

## Final review checklist

Before considering a change complete, verify:

### Architecture
- Are layer boundaries still clean?
- Did any infrastructure leak into a ViewModel?
- Did any business logic leak into a View or code-behind?

### Reuse
- Were related classes checked first?
- Was an existing pattern reused where appropriate?
- Was duplication reduced without inventing vague abstractions?

### ViewModel scope
- Is the ViewModel only modeling screen state and user intent?
- Does any logic belong lower in the stack?
- Is the ViewModel still easy to test?

### Extraction quality
- Was shared logic extracted only when justified?
- Does every new helper/service have a clear, specific responsibility?
- If a helper grew, was it split appropriately?

### Maintainability
- Are names explicit?
- Are methods reasonably small?
- Is async flow handled safely?
- Are error and busy states clear?

### Repo consistency
- Does the change match nearby patterns?
- Did the change avoid introducing a second style in the same area?
- Is the diff easy to review?

---

## Required final change summary

When you finish a non-trivial change, briefly state:
- which related classes or patterns were inspected
- whether logic was kept local, reused, or extracted
- why any new abstraction was introduced
- any architectural risk or follow-up worth noting