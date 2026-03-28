# OPENGL_AGENTS.md

## Purpose
This repository uses **OpenGL through OpenTK in C#**.

When changing rendering code, optimize for:
- correctness first
- explicit ownership
- predictable state changes
- easy debugging
- measurable performance work
- minimal hidden coupling

Assume a **desktop app** and a **modern OpenGL 4.5 core baseline** unless the repo explicitly states otherwise.

---

## Baseline assumptions
- Language: C#
- Binding: OpenTK
- API target: OpenGL 4.5 Core
- Platforms: Windows, optionally Linux
- Rendering style: shader-based, modern OpenGL

Do not introduce legacy OpenGL patterns.
Do not write new code using compatibility-profile concepts.

---

## Core rendering rules

### 1) Keep OpenGL code localized
OpenGL is a stateful API. Do not let `GL.*` calls spread across unrelated application code.

Use this separation:
- application/editor logic decides **what** should be rendered
- renderer decides **how** to issue GL commands
- resource wrappers own GL handles and their lifecycle
- shader/pipeline code owns shader contracts
- window/context code owns context creation and teardown

Do not call `GL.*` from random ViewModels, domain classes, tool logic, or data models.

### 2) Keep rendering architecture explicit
A rendering feature should clearly answer:
- which subsystem owns the GL objects
- which pass owns the draw calls
- which shader owns the interface contract
- which code uploads CPU data to GPU resources
- which code is responsible for disposal

If ownership is unclear, stop and improve the structure before adding more GL code.

### 3) Prefer renderer-facing abstractions over ad hoc calls
Good abstractions:
- `BufferObject`
- `Texture2D`
- `Framebuffer`
- `ShaderProgram`
- `Mesh`
- `RenderPass`
- `Material`
- `GpuUploadQueue`

Bad abstractions:
- `GLUtils`
- `RenderHelper`
- `GraphicsManager`
- `OpenGLCommon`
- giant static classes with unrelated methods

If a helper grows, split it by responsibility instead of adding more utility methods.

---

## OpenTK-specific rules

### 4) Request the intended context explicitly
Set the API version/profile explicitly when creating the window/context.
Do not rely on accidental defaults when the repo has a defined baseline.

If the repo baseline is OpenGL 4.5 Core:
- request 4.5 explicitly
- request core profile explicitly
- log actual version/vendor/renderer on startup
- fail clearly if the app cannot run on the obtained context

### 5) Keep context ownership centralized
The window/context layer owns:
- OpenGL context creation
- swapchain/present loop
- resize handling at the platform boundary
- shutdown order

Do not let random classes create hidden contexts or assume one is current.

### 6) All GL resource creation and disposal must happen with a valid current context
This is critical in C# because object lifetime is easy to obscure.

Do not:
- create GL objects from constructors that may run before context initialization
- delete GL objects from finalizers
- rely on GC timing for cleanup
- assume `Dispose()` can run safely from any thread at any time

Instead:
- create GL resources only after the context is ready
- dispose GL resources only while the correct context is current
- make shutdown order explicit
- route deferred destruction through the renderer if needed

### 7) Use `IDisposable` for GL resource wrappers
GL wrappers should usually implement `IDisposable`.

A wrapper should:
- own one clear GL handle or one tightly related resource group
- know whether it has already been disposed
- avoid double-delete
- never depend on a finalizer for correctness

Use finalizers only as a last-resort leak signal if the repo already has that pattern.
Do not make finalizers responsible for OpenGL cleanup.

### 8) Avoid hidden static GL state helpers
OpenTK exposes static `GL.*` entry points, but that does not mean rendering code should become static-global.

Do not hide ownership and state changes behind global static helper methods.
Keep resource operations and rendering operations attached to the subsystem that owns them.

---

## State and API usage rules

### 9) Prefer Direct State Access style
Write modern object-targeted code where possible.
Prefer APIs and structure that make it obvious which object is being modified.

Avoid large amounts of:
- bind
- mutate
- unbind
- hope nothing else changed

Even when OpenTK exposes both styles, prefer the clearer, object-directed pattern for a 4.5 codebase.

### 10) Keep state changes intentional
Make it obvious when code changes:
- current program
- current framebuffer
- viewport
- blend/depth/cull state
- bound textures/samplers
- draw topology

Do not bury state changes in low-level helpers that are called from unpredictable places.

### 11) Centralize per-pass state
Each render pass should clearly own its important state.
Examples:
- shadow pass
- geometry pass
- UI pass
- post-process pass

A pass should set the state it depends on rather than assuming some previous pass left the context in the right shape.

### 12) Avoid state churn without inventing fake abstractions
Reduce redundant state changes when practical, but do not build a complicated state cache unless the need is real.

Prefer:
- clear pass setup
- clear material binding
- measurable improvements

Do not guess about performance based on folklore.

---

## Resource wrapper rules

### 13) One wrapper, one responsibility
A resource wrapper should have a narrow job.

Good examples:
- `VertexBuffer`
- `IndexBuffer`
- `UniformBuffer`
- `Texture2D`
- `TextureAtlas`
- `Framebuffer`
- `Renderbuffer`
- `Shader`
- `ShaderProgram`

Do not create wrappers that mix:
- file loading
- shader compilation
- GL object ownership
- draw submission
- cache policy
- material behavior

### 14) Keep handles private
Raw integer GL handles should usually stay private to the wrapper/subsystem that owns them.
Expose higher-level operations instead of leaking handle manipulation across the codebase.

### 15) Make invalid states hard to represent
A disposed or not-yet-created resource should not look usable.

Prefer explicit lifecycle states over “magic zero means maybe fine”.
If a wrapper can exist before creation, make that state obvious.

### 16) Do not silently recreate GPU resources
If a resource must be rebuilt because of resize, format change, or device/context reset policy, make that path explicit.
Do not hide reallocation inside innocent-looking getters.

---

## Shader rules

### 17) Keep shader contracts explicit
A shader program is a contract between CPU-side code and GPU-side code.

Document and keep stable:
- vertex attributes
- uniform names or blocks
- storage buffers if used
- texture bindings
- outputs
- pass assumptions

If the shader contract changes, update the owning CPU-side code deliberately.

### 18) Check compile and link results every time
Shader compile and link diagnostics are required, not optional.
If compilation or linking fails:
- include the stage/program name
- include the info log
- fail loudly

Do not swallow shader errors.

### 19) Keep shader source/version handling intentional
Use a clear shader loading path.
Keep source organization predictable.
Do not scatter inline shader strings across many unrelated files unless the repo has a strong reason.

### 20) Avoid hidden uniform conventions
Do not rely on “everyone just knows” that a particular shader expects some matrix or texture slot.
Make binding and update code obvious in the owning renderer/material code.

---

## Buffer and upload rules

### 21) Choose an update strategy per buffer type
For each buffer path, be explicit whether it is:
- static
- rarely updated
- per-frame updated
- streaming

Do not mix multiple update strategies in the same path without a clear reason.

### 22) Keep upload ownership clear
Know exactly who owns CPU-to-GPU transfer for:
- mesh data
- dynamic instance data
- uniform data
- texture uploads
- readback paths

Do not let several unrelated systems write into the same buffer path casually.

### 23) Avoid accidental synchronization stalls
Dynamic uploads can stall the CPU if the GPU is still using the resource.
Design dynamic paths intentionally.

Do not use whole-pipeline blocking as a first fix.
Do not “solve” timing problems by making the app wait everywhere.

### 24) Prefer predictable update paths
Examples of good patterns:
- upload-once immutable/static mesh data
- dedicated dynamic buffer for per-frame data
- ring-buffer or staged upload path for frequent streaming
- explicit frame ownership for transient GPU data

Avoid ad hoc writes from many layers.

---

## Texture and framebuffer rules

### 25) Separate texture ownership from asset loading
A texture wrapper owns the GPU texture.
A loader/decoder owns file format reading and pixel decoding.
Do not merge these responsibilities unless the repo is intentionally tiny.

### 26) Keep format choice explicit
When creating textures or render targets, make these choices obvious:
- internal format
- size
- mip policy
- sampling policy
- render-target vs sampled usage

Do not hide important GPU format decisions behind generic helpers.

### 27) Keep framebuffer construction centralized
Framebuffer setup should live in one clear place per pass/subsystem.
Do not split attachments across many files and then hope the final combination is valid.

### 28) Validate framebuffer setup
Every framebuffer creation or resize path should validate that the framebuffer is complete before use.
Fail loudly with enough context to debug the issue.

### 29) Resize explicitly
Window-size-dependent resources should be recreated by an explicit resize path.
Do not let render targets auto-resize from random draw code.

---

## Debugging and diagnostics rules

### 30) Enable debug output in development
In development builds or debug modes:
- enable GL debug output if available
- register a debug callback
- surface errors and important warnings clearly

Do not leave debugging disabled while building major renderer features.

### 31) Label important objects
Label GPU objects that matter during debugging.
Examples:
- framebuffers
- textures
- buffers
- programs
- major passes

Make RenderDoc and driver/debug messages easier to interpret.

### 32) Group debug scopes by pass or operation
Use debug groups around:
- frame start/end
- shadow pass
- geometry pass
- post-processing
- uploads
- expensive or suspicious operations

Do not make the frame an anonymous blob of GL commands.

### 33) Log environment information on startup
At startup, log at least:
- GL version
- GLSL version
- vendor
- renderer

This makes hardware-specific debugging far easier.

---

## C# code quality rules for rendering code

### 34) Keep rendering code readable
Prefer explicit names and straightforward flow.
Good names:
- `UploadMeshData`
- `CreateShadowFramebuffer`
- `ResizeRenderTargets`
- `BindMaterial`
- `RenderOpaquePass`

Avoid vague names such as:
- `Handle`
- `Process`
- `UpdateStuff`
- `DoRender`

### 35) Keep methods focused
If a render method becomes too large, split it by:
- pass stage
- setup vs submission
- resource creation vs resource usage
- CPU data preparation vs GL issuing

Do not leave giant frame methods that do everything.

### 36) Avoid leaking OpenTK/OpenGL types upward
High-level application layers should not need to know about:
- `GL`
- framebuffer handles
- texture handles
- shader handles
- buffer handles
- low-level OpenTK rendering details

Keep those details inside renderer/resource layers.

### 37) Use collections and allocation patterns carefully
Per-frame garbage can become a hidden performance problem.
Avoid unnecessary allocations in hot render paths.

Do not micro-optimize everything prematurely, but do pay attention to:
- temporary array creation in per-frame code
- avoidable string building in hot paths
- repeated LINQ in inner loops
- hidden boxing in frequently called code

### 38) Use unsafe/pinning only when justified
If interop or upload performance requires spans, pinning, or unsafe code:
- keep it localized
- document why it is needed
- keep the safe boundary obvious

Do not spread unsafe patterns across unrelated code.

---

## ViewModel / app architecture boundary rules

### 39) Keep OpenGL out of ViewModels
If this repo uses MVVM or layered app architecture:
- ViewModels should not call `GL.*`
- ViewModels should not own GPU resources
- ViewModels should not know framebuffer/program/buffer details

A ViewModel may decide that a scene, document, or asset changed.
The renderer decides how GPU state/resources respond.

### 40) Keep render data flow explicit
A good pattern is:
- app/domain state changes
- renderer-facing scene/model representation updates
- renderer updates GPU resources as needed
- frame submission uses renderer-owned objects

Do not mix UI state management and OpenGL object management.

---

## Performance rules

### 41) Measure before broad refactors
Do not rewrite renderer architecture based on guesswork.
When changing performance-sensitive code, explain:
- what was slow
- what likely caused it
- what changed
- how the result was checked

### 42) Prefer debuggable performance improvements
Good improvements usually reduce:
- unnecessary resource recreation
- accidental synchronization
- redundant state churn
- excessive per-frame allocations
- repeated uploads of unchanged data

Do not trade away clarity for tiny unmeasured wins.

### 43) Be conservative with caching layers
Caches can help, but they can also make correctness and lifetime much harder.
Only add a cache when:
- ownership is clear
- invalidation is clear
- the performance need is real

Do not build vague global caches for “performance” without a clear plan.

---

## Refactoring triggers
Refactor when you see:
- `GL.*` calls spread across non-rendering classes
- resource wrappers with unclear disposal rules
- giant static helper classes
- framebuffers built from several unrelated locations
- repeated bind/mutate boilerplate everywhere
- shader interface assumptions hidden in comments or tribal knowledge
- per-frame allocations piling up in hot paths
- ViewModels or app services reaching into renderer internals
- huge renderer classes owning unrelated responsibilities

A growing helper often means a real subsystem is missing.

---

## Change workflow for agents
Before changing OpenGL/OpenTK code:
1. identify the owning renderer/pass/resource path
2. inspect nearby classes doing similar work
3. reuse the nearest good pattern if one exists
4. decide whether logic belongs in a wrapper, pass, material, or renderer service
5. keep disposal and context assumptions explicit
6. add diagnostics if the path is hard to debug

Do not start by adding `GL.*` calls wherever compilation happens to succeed.

---

## Final review checklist
Before finishing an OpenGL/OpenTK change, verify:

### Correctness
- Is the intended GL version/profile still explicit?
- Are all resources created only after context initialization?
- Are all resources disposed while a valid context is current?
- Are shader compile/link failures surfaced clearly?
- Are framebuffer setup and resize paths validated?

### Ownership
- Does each GL object have a clear owner?
- Is disposal explicit and safe?
- Did the change avoid hidden global state?

### Architecture
- Did GL stay inside rendering layers?
- Did ViewModels/app logic avoid renderer internals?
- Does the new code match nearby rendering patterns?

### Debuggability
- Is debug output usable in development?
- Are important objects or passes labeled/grouped?
- Is startup logging sufficient to identify driver/hardware issues?

### Maintainability
- Are names explicit?
- Are methods reasonably focused?
- Was duplication reduced without inventing vague helpers?
- If a helper grew, was it split by responsibility?

### Performance hygiene
- Are hot-path allocations reasonable?
- Is the upload/update path explicit?
- Were obvious synchronization stalls avoided?
- Was any optimization based on measurement rather than guesswork?

---

## Required final change summary
For non-trivial OpenGL/OpenTK changes, briefly state:
- which renderer/pass/resource code was inspected
- whether an existing pattern was reused
- which GL resources or shader contracts changed
- whether resource lifetime/disposal behavior changed
- whether upload/synchronization behavior changed
- any debugging hooks or labels added
- any version/profile assumptions introduced or reinforced
