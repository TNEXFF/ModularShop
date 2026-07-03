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
- Solution/root namespace: **`ModularShop`**. Host: `ModularShop.Server` (was `ModularShop.Api`).
  Kernel: `ModularShop.Kernel.{Domain,Application,Infrastructure,Web}` (was `ModularShop.SharedKernel[.*]`).
  Modules: `ModularShop.Modules.<Name>.{Domain,Application,Infrastructure,Api}` and `…​.<Name>.Contracts`.
- Schemas: lower‑case module name (`sales`, `warehouse`, `shipping`).
- One database: `ModularShopDemo`.
- Everything an example needs lives under `/mnt/d/TNEX/ModularShop` so the teaching repo is
  self‑contained.

---

## 5. Revision — layered projects, Ardalis, MediatR, real controllers (2026‑07‑02)

After the first version was reviewed, six changes were requested to move the example closer to a
production‑grade Clean‑Architecture MM. Each is recorded below; several **supersede** earlier decisions,
which is normal for an ADR log — the original reasoning is left intact above so the evolution is visible.

### D16 — One project **per layer** per module (supersedes D3’s "one project per module")
- **Choice:** every module is now four projects — `*.Domain`, `*.Application`, `*.Infrastructure`,
  `*.Api` — plus its `*.Contracts`. The Kernel is layered the same way. Inside each `*.Infrastructure`,
  the DbContext, EF configurations and migrations live under `Persistence/`.
- **Reasoning:** makes the Clean‑Architecture **dependency rule a compile‑time fact** (Domain can’t see
  EF; Api can’t see Infrastructure). This is the shape Grzybek’s reference sample uses and the shape the
  brief now asks for. Cross‑module encapsulation shifts from "`internal` in a single assembly" to "the
  project‑reference graph" — an equally strong compile‑time guarantee (a module can’t name another
  module’s `Product` because it doesn’t reference that assembly). Domain entities therefore become
  `public`; genuinely private types (DbContext, configs, seeds, handlers) stay `internal` to their layer.
- **Cost accepted:** more projects (19 total: 4 Kernel + host + 5 Sales + 5 Warehouse + 4 Shipping).
  Justified by the teaching goal (show the layering) and the intent to mirror Platform’s structure.

### D17 — `Ardalis.Result` for results (supersedes D11’s hand‑rolled `Result`)
- **Choice:** every use case returns an `Ardalis.Result<T>` (`Success` / `NotFound` / `Invalid(...)`).
  The kernel base controller (`ApiControllerBase`) maps `Result.Status` to the HTTP status code and the
  `ApiResponse<T>` envelope, which is **kept** (the brief requires controllers to return `ApiResponse`).
- **Reasoning:** the brief now asks to use the Ardalis packages instead of hand‑rolled equivalents. This
  is exactly what Platform does (`Ardalis.Result` + `ApiResponse`), so the mapping is now 1:1.

### D18 — `Ardalis.Specification` for the repository (supersedes D10’s thin hand‑rolled repository)
- **Choice:** the generic `EfRepository<T,TContext>` now derives from Ardalis
  `RepositoryBase<T>`; queries are `Specification<T>` objects defined in each module’s Application layer.
- **Reasoning:** once a module is split into layers, the Application layer must not reference the
  DbContext. Specifications are the clean, idiomatic Ardalis way to express queries in Application and run
  them in Infrastructure — avoiding a bespoke repository method per screen. (Ardalis.Specification.EFCore
  9.3.1 targets net9 but runs fine on net10 / EF Core 10 — verified by build + live run.)

### D19 — **MediatR** as the integration‑event bus (supersedes D8’s hand‑rolled bus)
- **Choice:** `OrderPlaced` is a MediatR `INotification`; handlers are `INotificationHandler<OrderPlaced>`
  in each subscribing module’s Infrastructure; the `PlaceOrder` use case publishes via `IPublisher`. The
  host registers MediatR once, scanning each module’s Infrastructure assembly for handlers.
- **Reasoning:** the brief now permits MediatR (a Community licence is available) and asks to prefer it
  over the hand‑rolled bus. MediatR 14’s Community tier is free for education/small orgs and only *logs*
  a notice without a key (never throws), so build‑and‑run is unaffected; `MediatR:LicenseKey` in config is
  the place to add a key. MediatR is used **only** for integration events — **not** as a command/query
  bus (still forbidden by the brief). Handlers stay thin (event → use case) so domain logic remains in
  Application.

### D20 — Real **controllers** invoking **use cases** (supersedes D9’s minimal APIs; refines D15)
- **Choice:** minimal APIs are replaced by MVC controllers in each module’s `*.Api` project. A controller
  injects **use‑case** classes (`PlaceOrder`, `ListProducts`, `ShipShipment`, …) — one class per
  operation — never an application "service". Controllers hold no logic: invoke a use case, return its
  `Result` as an `ApiResponse`. The host discovers controllers by registering each Api assembly as an MVC
  **ApplicationPart**.
- **Reasoning:** the brief asks for real controllers that invoke use cases (not services). Splitting the
  former `OrderService`/`ProductService`/… into single‑responsibility use cases makes each operation a
  first‑class, independently testable unit and reads clearly against the Clean‑Architecture layering.

### D21 — **Swagger** (Swashbuckle) enabled
- **Choice:** `Swashbuckle.AspNetCore` provides Swagger/OpenAPI UI at `/swagger`, enabled in every
  environment for this demo.
- **Reasoning:** the brief asks to configure Swagger; Swashbuckle is the familiar choice for a .NET team
  and, with `[ApiController]` controllers, needs no per‑endpoint annotation to list all 10 routes.

### D22 — Rename **Kernel** and **Server**; bump Node/pnpm
- **Choice:** `SharedKernel*` → `Kernel.*` (drop "Shared"); `ModularShop.Api` → `ModularShop.Server`.
  Frontend toolchain updated to **Node 24 / pnpm 11** (from Node 20 / pnpm 9); the SPA build output moved
  to `src/ModularShop.Server/wwwroot`. pnpm 11 gates package build scripts, so `client/pnpm-workspace.yaml`
  allows `esbuild` to run non‑interactively.
- **Reasoning:** naming per the brief; version bumps to match the updated local toolchain.

---

## 6. Revision — "Option B": one host context, centralised migrations, Identity, a shared kernel model, and an independent module (2026‑07‑03)

The example was reshaped to match the **target design for migrating `../Platform`**: instead of a
context‑per‑module *at runtime* (the classic MM default, "Option A"), a **single host context absorbs
per‑module blueprints** ("Option B", the shape Platform’s `MainDbContext` + `IModuleEntityProvider`
already leans toward). Several earlier decisions are superseded; the originals are left intact above so
the evolution stays visible.

### D23 — A single **host DbContext** built from per‑module **blueprints** (supersedes D5’s per‑module runtime context; refines D16)
- **Choice:** every module keeps its own `DbContext`, but only as an *organisational blueprint*
  (DbSets only, never instantiated). At runtime there is **one** context, `ModularShopDbContext` in the
  host, deriving from the kernel’s Identity base. It absorbs each module through a new
  `IModuleModel { Schema; ContextType; Configure(ModelBuilder); }`: `ModuleModelBuilder.ApplyModuleModel`
  **reflects** the blueprint’s `DbSet<T>` properties to register the module’s ordinary entities, then
  calls the module’s one `Configure` for special mapping. Every service injects the base `DbContext`,
  aliased to the host context.
- **Reasoning:** this is exactly the user’s vision — *context‑per‑module for organisation and schemas,
  one context for runtime*. It removes "which context do I inject?", makes cross‑module reads/writes a
  single change‑tracker/transaction, and lets the host own migrations — while each module still has an
  obvious, self‑contained place to declare its tables. Reflection means the DbSets are the recipe, so a
  module never lists its entities twice, and there are **no per‑entity configuration classes**.
- **Alternatives:** keep context‑per‑module at runtime (Option A — simpler isolation, but reintroduces
  the injection ambiguity, multiple migration chains, and no single transaction across a request);
  hand‑write `IEntityTypeConfiguration<T>` per entity and have the host apply them (more classes than the
  user wanted). Rejected in favour of blueprint‑reflection + one `Configure` per module.

### D24 — **Centralised migrations**, owned by the host (supersedes D5’s per‑module migrations)
- **Choice:** one migration chain in `ModularShop.Server/Migrations`, generated with the official
  `dotnet ef` tool against `ModularShopDbContext`. A design‑time factory builds the host context with the
  same module list the runtime uses (`HostModules`), so one `migrations add` covers every module’s tables.
  Schema‑per‑module is preserved by assigning each entity’s schema **after** the model is built, by the
  assembly it lives in (`ApplyModuleSchemas`); one `dbo.__EFMigrationsHistory` remains.
- **Reasoning:** with a single context the host is the natural migration owner, and one chain is simpler
  to reason about and to apply at startup than N per‑module chains. Child entities (OrderLine, …) land in
  the right schema automatically because they share their module’s Domain assembly.
- **Alternatives:** per‑module migrations (Option A) or Grzybek‑style centralised SQL scripts. Rejected:
  the EF‑tool single chain is the least‑ceremony fit here.

### D25 — Use cases depend on **`DbContext`** directly; **remove Ardalis.Specification + the repository** (supersedes D10 and D18)
- **Choice:** every use case injects the base `DbContext` and queries with plain LINQ
  (`db.Set<T>()…Include…AsNoTracking`). `EfRepository`, `IRepositoryBase<T>`, and all `*Specs.cs`
  specification classes were deleted; the Application layer now references EF Core.
- **Reasoning:** the user asked for services to "work only with `DbContext`" and to drop the
  specifications as unnecessary. With one shared context, a repository/specification indirection earns
  little — a direct `DbContext` seam is easy to read and enough for this domain. This is a **deliberate
  relaxation** of Clean Architecture (Application now knows EF), made consciously because it doesn’t pay
  for itself here.
- **Cost accepted:** the Domain‑purity rule (Application must not see EF) is given up. Kept `Ardalis.Result`
  (D17) — it is unrelated to querying and still maps `Result`→`ApiResponse`.

### D26 — **Shared kernel entities**: `Customer` and `Currency` (nuance to D4’s "lean kernel")
- **Choice:** promote `Customer` (used by Sales, Shipping, Support) and add `Currency` (used by Warehouse
  and Sales) into `Kernel.Domain`. Modules reference them by **cross‑schema foreign key**; those FKs are
  the only cross‑schema references in the database.
- **Reasoning:** the kernel is the right home for entities *two or more modules must agree on* — that is
  what keeps a customer/currency consistent across the whole system, and it’s exactly the "centralised
  entities used by multiple modules" the user wants. The FK to a kernel entity is the deliberate signal
  "shared data", as opposed to another module’s *private* data (reached only via a contract). The kernel
  stays lean: only genuinely shared reference entities go here, never a module’s own business entities.
- **Alternatives:** keep Customer in Sales and let others snapshot it (Option A’s stance — fine for
  decoupling, but doesn’t demonstrate a shared, consistent entity, which the brief asked for); promote
  `Product` too (rejected — it would undermine the deliberate Sales↔Warehouse snapshot/contract lesson).

### D27 — **ASP.NET Core Identity in the kernel**, cookie auth, whole app gated (supersedes D13’s header‑based `ICurrentUser`)
- **Choice:** add ASP.NET Core Identity (`ApplicationUser`/`ApplicationRole`, `KernelDbContext :
  IdentityDbContext`, tables in the `kernel` schema). A kernel `AuthController` does register/login/
  logout/me with **cookie** sign‑in returning the `ApiResponse` envelope; `ApiControllerBase` carries
  `[Authorize]`, so every module endpoint requires a signed‑in user; `ICurrentUser` now reads the
  authenticated principal. Seeded demo users (`admin@`, `agent@`, password `Passw0rd!`).
- **Reasoning:** the brief asked to replace the placeholder auth with the real Identity library, in the
  kernel. Authentication is the archetypal cross‑cutting concern, so the kernel owns it and the single
  host context persists it. Cookie auth is the simplest fit for a same‑origin SPA.
- **Alternatives:** `MapIdentityApi` built‑in endpoints (less code, but a non‑`ApiResponse` shape); JWT
  bearer (more moving parts for a same‑origin SPA). Rejected in favour of a small, envelope‑consistent,
  cookie‑based `AuthController`.

### D28 — A **genuinely independent** module: **Support** (extends D1)
- **Choice:** add a fourth module, **Support** (customer‑service tickets: `Ticket` + `TicketMessage`),
  that references **no** other module, publishes/consumes **no** integration events, and has **no**
  `*.Contracts`. It uses only the kernel — the shared `Customer` and the signed‑in Identity user — and
  owns its own `support` schema.
- **Reasoning:** Sales/Warehouse/Shipping deliberately collaborate; Support deliberately does not. A
  heterogeneous set (modules that must collaborate *and* modules that merely coexist) is a truer model of
  Platform, whose modules range from tightly related to completely independent. Support is where the
  "unrelated module + shared kernel entity" pattern is shown cleanly.
- **Alternatives considered:** a Content/CMS module (even more isolated but doesn’t exercise the shared
  entity or Identity), Notifications (tends to couple to other modules’ events). Support hit the sweet
  spot of *unrelated domain* + *uses the kernel*.

### D29 — Explicitly **skipped** for this iteration
Per the brief, three otherwise‑natural additions were left out on purpose: the **architecture "tripwire"
test** (module A can’t reference module B’s entity namespaces), a **startup check that every enabled
module’s entities are actually mapped**, and a mechanism for modules to **opt into** the shared kernel
behaviours. Each is noted in `architecture.md` as a next step.

### What carried over unchanged
Clean‑Architecture layering per module and in the kernel (D16), `Ardalis.Result`→`ApiResponse` (D17),
**MediatR** for integration events only (D19), real **controllers invoking use cases** (D20), **Swagger**
(D21), the two inter‑module communication styles (D7), and the `.slnx`/naming/toolchain conventions (D22).

---

## 7. Revision — our own repository layer (2026‑07‑03)

D25 relaxed Clean Architecture by letting the Application layer depend on EF Core (use cases injected the
`DbContext`). That went further than "remove the specifications" required. This revision keeps
`Ardalis.Specification` **removed** but restores a repository — a hand-rolled one, modelled on Platform's
`IRepository<T>`/`Repository<T>` and the sibling Social-Media-Platform kernel — so the Application layer no
longer references EF Core.

### D30 — Hand-rolled repositories + Unit of Work (supersedes D25's repository removal; keeps its Specification removal)
- **Choice:** `IReadRepository<T>` + `IRepository<T>` live in `Kernel.Domain/Repositories` (constraint
  `where T : Entity`); `IUnitOfWork` in `Kernel.Application`. Implementations are in `Kernel.Infrastructure`:
  a **public** `ReadRepository<T>` → `Repository<T>` bound to the base `DbContext` (the one host context),
  registered **open-generic** so a single implementation serves every module's entities; plus `UnitOfWork`
  (translates `DbUpdateConcurrencyException` → `DatabaseUpdateException`). Reads are **materialised + async**
  (the Application never sees `IQueryable`) and `NoTracking` by default; `GetByIdAsync` / `GetByIdsAsync` /
  `GetForUpdateAsync` are **tracked** for load-then-modify. Includes are **typed and string** (the latter for
  cross-cutting navigations). Committing is the unit of work's job, not the repository's. All four module
  `*.Application` projects **dropped the EF Core package**.
- **A specific repository only where it earns it:** Support's `ITicketRepository.ListSummariesAsync`
  projects a message **count** in the database (plain-LINQ correlated sub-query, no raw SQL) instead of
  loading every message body — the generic repository would be both inefficient and the wrong shape for the
  ticket list. Sales / Warehouse / Shipping use only the generic repository; that contrast *is* the lesson.
- **Reasoning:** dropping the specifications was right, but deleting the repository *and* pulling EF Core into
  Application was too large a break with Clean Architecture for a teaching reference. A thin repository is a
  recognisable seam, mirrors Platform, and — with one host context — costs almost nothing.
- **Cost / alternatives:** `IReadRepository<>` and `IRepository<>` both map to the one `Repository<>`, so
  resolving both in a scope yields two repository instances sharing the scope's single `DbContext` — no
  correctness issue (the context is the unit of tracking). Baking `SaveChanges` into the repository
  (Platform's style) was rejected for a separate `IUnitOfWork` (the sibling kernel's cleaner split).
  `Currency` (string-keyed reference data, not an `Entity`) stays seed-only and needs no repository.

### D31 — Domain entities have **client-assigned** keys (`ValueGeneratedNever`)
- **Choice:** a global model convention (`ModuleModelBuilder.ApplyClientAssignedKeys`, applied by the host
  context after the module models) marks every `Entity`-derived type's `Id` as client-assigned.
- **Reasoning:** each entity sets its own `Guid` `Id` in its constructor, but EF Core's default Guid
  convention (`ValueGeneratedOnAdd`) mis-detects a **new child added to an already-tracked parent** (e.g. a
  `TicketMessage` added to a loaded `Ticket`) as an existing row — it issues an `UPDATE` affecting 0 rows and
  throws a concurrency error instead of inserting. This latent defect (present since the entities were
  written; surfaced by end-to-end validation of `AddTicketMessage`) is fixed by telling EF the key is
  client-assigned. The column is unchanged — the Guid is generated in memory either way — so no schema change
  or new migration results (`InitialCreate` was regenerated to keep the model snapshot in sync). Identity's
  string/int keys and the code-keyed `Currency` are untouched.

_Last updated: 2026‑07‑03._
