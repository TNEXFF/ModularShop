# Decision Log — ModularShop (Modular Monolith teaching example)

This log records **every significant architectural decision** made while building `ModularShop`,
the reasoning behind it, and the alternatives that were considered and rejected. It is written to
be read top‑to‑bottom before the code, so a mid‑level developer can understand *why* the solution
looks the way it does.

> **Guiding principle for the whole project:** prefer the *simplest* implementation that still
> **correctly** demonstrates a Modular Monolith (MM). When production complexity conflicts with
> educational clarity, clarity wins — but the result must never be *architecturally wrong*.

---

## 0. Context & goals

- **Goal:** a small, correct, runnable MM in **ASP.NET Core (Web API) + React**, on **MSSQL**, that
  teaches the core MM concepts and can be presented to coworkers as the *target design* for
  migrating the existing `./Platform` codebase.
- **Inputs studied:** the `./Platform` solution (a real, mid‑transition MM), Kamil Grzybek's
  `./Modular Monolith with DDD` reference sample, and community sources (Grzybek, Milan Jovanović,
  Ardalis/NimblePros, Mukesh Murugan, Microsoft docs). See `platform-mapping.md` for what we took
  from Platform and `architecture.md` for the concepts taught, with sources.
- **Hard constraints (from the brief):** MSSQL; migrations via the official EF tool only; no Docker;
  pnpm for the frontend; **no CQRS command/query buses**; **no test projects**; MediatR only if it
  earns its place; a **Shared/Kernel module that itself follows Clean Architecture**; generous seed
  data; minimal extra NuGet/npm packages.

---

## 1. Environment & tooling (verified before any code)

| Concern | Finding | Decision |
|---|---|---|
| .NET SDK | `dotnet` in this WSL shell is a wrapper that invokes the **Windows .NET 10 SDK** (`win-x64`). | Use it as‑is. The backend builds/runs as a Windows process, which is exactly how the user runs it in Visual Studio. |
| EF tooling | `dotnet ef` 10.0.9 works. | Migrations are created with the **official tool** (satisfies the constraint). `migrations add` needs only the model, not a live DB. |
| Database | **SQL Server 2022 Developer** is installed on Windows (default instance). TCP/IP is **disabled**, but a *Windows* process reaches it over **shared memory** via `Server=localhost`. `CREATE`/`DROP DATABASE` verified. | Use `Server=localhost;…;Trusted_Connection=True;TrustServerCertificate=True`. No Docker, no TCP changes required. |
| Node / pnpm | Node 20 + a Linux‑native **pnpm 9** (installed via `npm i -g pnpm@9`; the pre‑existing pnpm 11 required Node ≥22 and was broken). | Frontend uses pnpm 9 + Vite. |

**Why this matters:** because the backend runs as a Windows process, it can talk to the local SQL
Server *without* enabling TCP or using Docker — so the full order→shipment flow is genuinely
runnable and verifiable in this environment. See `README.md` for the exact commands.

---

## 2. Architecture validation (done *before* scaffolding)

The brief requires validating module boundaries, dependency direction, shared‑kernel
responsibilities, and inter‑module communication before implementing. Result of that validation:

- **Module boundaries — VALID.** Modules are **business capabilities** (Sales, Warehouse, Shipping),
  not technical layers. This is the single most important MM rule (see `architecture.md` §"module =
  vertical slice"). A "data‑access module" or "UI module" would have been the layered‑monolith trap
  and is explicitly rejected.
- **Dependency direction — VALID / acyclic.** A module may reference **only** the Shared Kernel and
  other modules' **`.Contracts`** projects — never another module's implementation. Concretely:
  `Sales → Warehouse.Contracts` (synchronous price/stock query) and `Warehouse → Sales.Contracts`,
  `Shipping → Sales.Contracts` (they handle the `OrderPlaced` event). Because the `.Contracts`
  projects depend on nothing but the Shared Kernel, there is **no cycle**. The `Api` host references
  every module; no module references the host.
- **Shared‑kernel responsibilities — VALID / lean.** Only truly shared primitives and cross‑cutting
  infrastructure live there (base `Entity`, `IIntegrationEvent`, the event bus, `IModule`, the
  generic repository, the `ApiResponse` envelope, exception handling, a light `ICurrentUser`). No
  business logic. An over‑fat kernel would re‑couple modules — explicitly avoided.
- **Inter‑module communication — VALID / both styles present.** Synchronous, request/response calls
  go through a module's **public interface** (`IWarehouseApi`); "something happened" notifications
  go through **asynchronous in‑process integration events** (`OrderPlaced`). One realistic flow
  exercises both, which is the whole point of the example.

No architectural issues were found that required changing the plan. Details and the exact reference
rules are enforced in code (project references + `internal` types) and documented in
`architecture.md`.

---

## 3. Decisions (ADR style)

Each decision lists the **choice**, the **reasoning**, and the **alternatives considered**.

### D1 — Demo domain: a tiny e‑commerce shop with an Order → Shipment flow
- **Choice:** three business modules — **Sales** (customers, orders), **Warehouse** (products,
  stock), **Shipping** (shipments) — and one end‑to‑end flow: *place an order → validate price/stock
  → record the order → decrement stock → create a shipment.*
- **Reasoning:** the flow is universally understood and, crucially, it needs **both**
  communication styles: a *synchronous* "what is the price/stock right now?" query (Sales → Warehouse)
  and an *asynchronous* "an order was placed" notification (Sales → Warehouse + Shipping). One flow
  therefore teaches encapsulation, schema‑per‑module, the composition root, the shared kernel, and
  both integration styles.
- **Alternatives:** the DDD sample's Meetings/Payments/Registrations domain (richer but heavier and
  less universally relatable); a single‑module CRUD app (too trivial to show *inter*‑module
  communication — the most important part). Rejected in favour of the smallest domain that still
  forces genuine cross‑module interaction.

### D2 — A module is a *vertical business slice*, organised by feature inside
- **Choice:** each module owns its domain, data, use cases, and HTTP endpoints. Inside a module,
  code is grouped by feature, not by global technical layers.
- **Reasoning:** community consensus (Grzybek, Jovanović): modules define *boundaries*; vertical
  slices organise code *within* a module. Business changes then touch one module, not every layer.
- **Alternatives:** classic horizontal layering across the whole app (the traditional layered
  monolith) — rejected because it makes every feature change cut across all layers and provides no
  real encapsulation.

### D3 — Enforce boundaries with **separate projects + `internal` + a `.Contracts` public surface**
- **Choice:** every module is its own project; its domain/EF/handlers are `internal`; the *only*
  public surface is a small `X.Contracts` project (interfaces, DTOs, integration events). A module
  references only the Shared Kernel and other modules' `.Contracts`.
- **Reasoning:** this makes the boundary a **compile‑time** fact, not a convention. If Sales tried to
  use Warehouse's `Product` entity, it wouldn't compile — the best kind of enforcement for teaching.
  Modules that expose nothing (Shipping) simply have no `.Contracts` project, which teaches that not
  every module must have a public API.
- **Alternatives considered:**
  - *One project per module with folders only* (Mukesh's simplest form): fewer projects, but `public`
    types leak across modules and nothing stops a bad reference. Rejected: weaker teaching of
    encapsulation.
  - *Architecture tests* (NetArchTest/ArchUnitNET) to fail the build on boundary violations: the
    ideal belt‑and‑suspenders, **but the brief forbids test projects.** Documented as the natural
    next step in `architecture.md`; boundaries here are enforced by references + `internal`.
  - *Grzybek's per‑layer assemblies per module* (`.Domain/.Application/.Infrastructure/.IntegrationEvents`
    = 4 projects × N modules): correct but heavy; rejected for a beginner example.

### D4 — Shared Kernel as **three Clean‑Architecture‑layered projects**
- **Choice:** `SharedKernel` (domain/core primitives — no framework deps), `SharedKernel.Infrastructure`
  (EF base context, generic repository, in‑process event bus, current‑user), `SharedKernel.Web`
  (the `ApiResponse` envelope, exception‑handling middleware, endpoint helpers). Dependencies point
  inward: `Web → Infrastructure → Core`.
- **Reasoning:** the brief explicitly requires a shared kernel *whose projects follow Clean
  Architecture*, mirroring Platform's `TNEX.Core / TNEX.Infrastructure / TNEX.Api`. Splitting by layer
  makes the dependency rule visible and gives a clean home for logging/identity/cross‑cutting code.
- **Alternatives:** a single `Shared` project (simpler, but doesn't demonstrate the required Clean
  Architecture layering and mixes domain with ASP.NET types); Ardalis.SharedKernel as a NuGet package
  (great in production, but hides the very code we want students to read). Rejected for clarity.
- **Guardrail:** the kernel stays *lean*. It holds primitives and cross‑cutting infra only — never
  business rules — to avoid re‑coupling modules through a fat kernel.

### D5 — Persistence: **one database, one schema per module, one DbContext per module**
- **Choice:** a single MSSQL database `ModularShopDemo`; `SalesDbContext` → schema `sales`,
  `WarehouseDbContext` → schema `warehouse`, `ShippingDbContext` → schema `shipping`, each via
  `modelBuilder.HasDefaultSchema(...)` on a shared `ModuleDbContext` base. Each context owns its own
  EF migrations.
- **Reasoning:** this is the consensus default for MM (Jovanović's "logical isolation via schemas",
  Grzybek's "each module has its own state, in its own schema"). It gives real data ownership and
  makes boundaries obvious, while keeping single‑database transactions and simple ops. One DbContext
  per module is what prevents the #1 MM anti‑pattern — a shared DbContext.
- **Alternatives:** shared DbContext (the classic mistake — rejected outright); database‑per‑module
  (more isolation but more ops overhead — noted as the later step toward extracting a microservice,
  not needed here).

### D6 — Composition root: a thin host + a small **`IModule`** bootstrapper per module
- **Choice:** the Shared Kernel defines `IModule { Register(services, config); MapEndpoints(app); }`.
  Each module implements it (`SalesModule`, …). The `Api` host discovers the modules, calls
  `Register` on each at startup and `MapEndpoints` on each when building the pipeline, and contains
  **no business logic**.
- **Reasoning:** each module owns its own registration ("its own composition root"); the host just
  wires them together. This is a deliberately **simplified** version of Platform's rich `IModule`
  (which also had `RegisterDbContext`, `RegisterControllers`, `InitializeAsync`, `ShutdownAsync`,
  permissions, frontend metadata, reflection discovery). We keep the essence and drop the ceremony.
- **Alternatives:** per‑module `AddXModule()` extension methods called explicitly by the host
  (Mukesh's style — also fine; we fold this into `IModule.Register` so the pattern is uniform);
  full reflection‑based auto‑discovery like Platform (elegant but adds "magic" that obscures the
  wiring for beginners — we register an explicit module list in one readable place instead).

### D7 — Inter‑module communication: **two explicit styles**
- **Choice:**
  1. **Synchronous public API** — `Warehouse.Contracts.IWarehouseApi` (implemented `internal` in
     Warehouse, injected into Sales). Used when Sales needs an answer *now* (current price + stock)
     before creating an order.
  2. **Asynchronous integration event** — `Sales.Contracts.OrderPlaced` published on an in‑process
     event bus after the order commits; Warehouse decrements stock and Shipping creates a shipment
     in their own handlers, each writing to its own schema.
- **Reasoning:** these are the two styles every MM must demonstrate. Rule of thumb taught by the
  example: *need a value back now → synchronous interface; "this happened", fire‑and‑forget →
  integration event.* Contracts are the module's public API; entities never cross the boundary
  (DTOs only).
- **Alternatives:** async‑only (Grzybek's canonical repo forbids direct calls) — architecturally
  purer but hides the very common and legitimate synchronous case; sync‑only — can't teach loose,
  event‑driven coupling. We show both, which is the pragmatic middle the broader community teaches.

### D8 — A **hand‑rolled in‑process event bus** instead of MediatR
- **Choice:** a ~40‑line `IEventBus` in the Shared Kernel that resolves `IIntegrationEventHandler<T>`
  implementations from DI and invokes them. No MediatR.
- **Reasoning:** three reasons, all aligned with the brief. (1) *Transparency* — students can read
  the entire mechanism; the integration‑event concept isn't hidden behind a library. (2) *Fewer
  dependencies* — the simplicity principle says don't add a package unless it earns its place, and a
  tiny bus does. (3) MediatR moved to a **commercial license in 2025**, so avoiding it sidesteps a
  real‑world licensing wrinkle. The brief allows MediatR "if necessary" — here it isn't.
- **Alternatives:** MediatR `INotification` (the community‑standard implementation — noted in the
  docs as a drop‑in replacement, and the code is shaped so swapping it in is trivial); an external
  broker/outbox (correct for production reliability, but far too heavy for a teaching example —
  documented as the production upgrade).

### D9 — **Minimal APIs** instead of MVC controllers
- **Choice:** each module maps its endpoints via `IModule.MapEndpoints` using minimal APIs and
  endpoint groups (e.g. `/api/orders`).
- **Reasoning:** less ceremony, less boilerplate, and it keeps a module's endpoints co‑located with
  its feature code (reinforcing vertical slices). Ideal for teaching.
- **Alternatives:** MVC controllers (what Platform uses; more familiar to some teams but more
  ceremony and an extra discovery mechanism). Noted in `platform-mapping.md` as an easy swap.

### D10 — A thin generic repository, but no heavyweight patterns
- **Choice:** `IRepository<T>` + `EfRepository<T>` in the Shared Kernel for the common
  add/get/list/save cases; modules use it (or the `DbContext` directly for module‑specific queries).
- **Reasoning:** the repository is a recognisable seam and mirrors Platform's `IRepository<T>` /
  `Repository<T>`, but we keep it minimal (no `IGetAll`, no specification objects, no Unit of Work).
  Reads default to `AsNoTracking` — a good habit borrowed from Platform.
- **Alternatives:** Ardalis.Specification (powerful, but adds a package and a concept students don't
  need yet); no repository at all / raw DbContext everywhere (fine, but the repository is a useful,
  familiar teaching seam and eases the Platform mapping). Balanced choice.

### D11 — A tiny hand‑written `Result` + `ApiResponse<T>` envelope (no Ardalis NuGet)
- **Choice:** implement a small `Result`/`Result<T>` and an `ApiResponse<T>` envelope
  (`IsSuccess`, `Message`, `Errors`, `Data`) in `SharedKernel` / `SharedKernel.Web`, matching the
  *shape* of Platform's `Ardalis.Result` + `ApiResponse<T>`.
- **Reasoning:** the brief calls out Platform's Ardalis/`ApiResponse` as candidates. We adopt the
  **pattern** (consistent success/failure without exceptions for control flow, a uniform JSON
  envelope) but implement it in ~50 transparent lines rather than taking the dependency — students
  see exactly what it does, and the Platform mapping is explicit.
- **Alternatives:** `Ardalis.Result` + `Ardalis.Result.AspNetCore` (exactly what Platform uses; a
  one‑line swap, documented). Rejected only to keep the example dependency‑free and transparent.

### D12 — Integration events handled **in‑process, synchronously, after commit** (no outbox)
- **Choice:** Sales commits the order, then publishes `OrderPlaced`; the bus invokes the Warehouse
  and Shipping handlers within the same request, each committing to its own schema.
- **Reasoning:** simplest correct mechanism for a single‑process demo; the flow is easy to trace in a
  debugger.
- **Alternatives / known trade‑off:** a transactional **outbox** guarantees the event isn't lost if
  the process dies mid‑handling. That reliability is a real production concern but pure infrastructure
  noise for teaching — it is called out in `architecture.md` as the first production upgrade.

### D13 — Cross‑cutting concerns (logging, identity) live in the Shared Kernel, kept light
- **Choice:** built‑in `Microsoft.Extensions.Logging` (no Serilog dependency) + a small
  request‑logging + exception‑handling middleware in `SharedKernel.Web`; a light `ICurrentUser`
  populated from an `X-User-Id` header (default seeded user) used to stamp `Order.PlacedBy`.
- **Reasoning:** demonstrates *where* logging/identity/authorization belong in an MM (the shared
  kernel, exactly as Platform intends with `TNEX.Api`/`TNEX.Core`) without the weight of full
  ASP.NET Identity + JWT/OIDC. The seam is real; swapping in real auth is a documented step.
- **Alternatives:** full Identity + JWT (too much ceremony for the teaching goal); Serilog (nice, but
  an extra dependency the example doesn't need). Both noted as upgrades.

### D14 — Frontend: **Vite + React + TypeScript**, pnpm, minimal deps, feature folders
- **Choice:** a Vite React‑TS app under `client/`, using `pnpm`, with `src/features/{catalog,orders,shipments}`
  mirroring the backend modules; plain fetch for the API and hand‑written CSS (no UI kit, no axios,
  no state library). In production the API host serves the built SPA (**one deployment unit** — the
  core MM selling point); in dev, Vite proxies `/api` to the host.
- **Reasoning:** keeps the React surface small so attention stays on the architecture, while the
  feature folders reinforce the modular structure on the client. Serving the built SPA from the API
  makes "single deployable unit" concrete.
- **Alternatives:** Next.js (Platform uses it — more machinery than this demo needs); Tailwind/MUI
  (extra build/deps for little teaching value here). Rejected for simplicity.

### D15 — Explicitly **out of scope** (with reasons)
- **CQRS command/query buses** — forbidden by the brief and unnecessary; endpoints call module
  services directly. **Test projects** — forbidden by the brief; architecture tests are noted as the
  ideal boundary guard. **Docker** — forbidden; we run against local SQL Server. **Outbox/inbox,
  Quartz, event sourcing, Autofac, IdentityServer, Dapper read models, strongly‑typed IDs, module
  Federation/plugin loading** — all present in the reference samples but deliberately omitted as
  production/enterprise concerns that bury the fundamentals (see `platform-mapping.md`).

---

## 4. Naming & layout conventions
- Solution/root namespace: **`ModularShop`**. Host: `ModularShop.Api`. Shared: `ModularShop.SharedKernel[.Infrastructure|.Web]`.
  Modules: `ModularShop.Modules.<Name>` and `ModularShop.Modules.<Name>.Contracts`.
- Schemas: lower‑case module name (`sales`, `warehouse`, `shipping`).
- One database: `ModularShopDemo`.
- Everything an example needs lives under `/mnt/d/TNEX/ModularShop` so the teaching repo is
  self‑contained.

_Last updated: 2026‑07‑01._
