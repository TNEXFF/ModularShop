# Decision Log — ModularShop (Modular Monolith teaching example)

This log records the **significant architectural decisions** that define `ModularShop` as it stands
today, the reasoning behind each, and the main alternative that was rejected. It is written to be read
top‑to‑bottom before the code, so a mid‑level developer can understand *why* the solution looks the way
it does. A short **Evolution** appendix at the end lists the ideas that were tried and superseded along
the way.

> **Guiding principle for the whole project:** prefer the *simplest* implementation that still
> **correctly** demonstrates a Modular Monolith (MM). When production complexity conflicts with
> educational clarity, clarity wins — but the result must never be *architecturally wrong*.

---

## 0. Context & goals

- **Goal:** a small, correct, runnable MM in **ASP.NET Core (Web API) + React**, on **MSSQL**, that
  teaches the core MM concepts and can be presented to coworkers as the *target design* for migrating
  the existing `./Platform` codebase.
- **Inputs studied:** the `./Platform` solution (a real, mid‑transition MM), Kamil Grzybek's
  `./Modular Monolith with DDD` reference sample, and community sources (Grzybek, Milan Jovanović,
  Ardalis/NimblePros, Mukesh Murugan, Microsoft docs). See `platform-mapping.md` for what we took from
  Platform and `architecture.md` for the concepts taught.
- **Hard constraints (from the brief):** MSSQL; migrations via the official EF tool only; no Docker;
  pnpm for the frontend; **no CQRS command/query buses**; **no test projects**; a **Shared/Kernel that
  itself follows Clean Architecture**; generous seed data; minimal extra NuGet/npm packages.

---

## 1. Environment & tooling

| Concern | Reality | Consequence |
|---|---|---|
| .NET SDK | `dotnet` in this WSL shell wraps the **Windows .NET 10 SDK** (`win-x64`). | The backend builds/runs as a Windows process — exactly how it runs in Visual Studio. |
| EF tooling | `dotnet ef` 10.0.9. | Migrations use the **official tool**; `migrations add` needs only the model, not a live DB. |
| Database | **SQL Server 2022 Developer** on Windows (default instance); TCP/IP disabled, reached over **shared memory** via `Server=localhost`. | `Server=localhost;…;Trusted_Connection=True;TrustServerCertificate=True`. No Docker, no TCP changes. |
| Node / pnpm | **Node 24** + a Linux‑native **pnpm 11**. | Frontend uses pnpm + Vite. |

**Why this matters:** because the backend runs as a Windows process, it talks to the local SQL Server
*without* enabling TCP or using Docker — so the full order→shipment flow is genuinely runnable and
verifiable in this environment.

---

## 2. Decisions (current architecture)

Each decision lists the **choice**, the **reasoning**, and the main **alternative rejected**.

### D1 — A module is a *vertical business slice*, organised by capability
Modules are **business capabilities** (Sales, Warehouse, Shipping, Support), each owning its domain, data
and API end‑to‑end. *Rejected:* technical‑layer "modules" (a data‑access or UI module) — the
layered‑monolith trap that couples everything through a shared technical layer.

### D2 — Boundaries enforced by separate projects + `internal` + a `.Contracts` surface
Each module is several projects; the only thing it exposes to other modules is a tiny `*.Contracts`
assembly. A module references **only** the Kernel and other modules' `*.Contracts` — never another
module's implementation — so the reference graph is acyclic and boundaries are a compile‑time fact.
`Warehouse.Contracts` = `IWarehouseApi` + `ProductStock`; `Sales.Contracts` = the `OrderPlaced` event;
Shipping and Support expose none. *Rejected:* one project per module with `internal` alone — insufficient
once modules must reference each other's public surface. *Next step:* a NetArchTest "tripwire" (omitted
per the brief).

### D3 — Clean Architecture inside each module (Domain / Application / Infrastructure / Api)
Dependencies point inward; Application holds the use cases and depends on abstractions, not EF Core.
*Rejected:* a flat module — loses the inner testability and dependency‑direction the example is meant to
teach.

### D4 — The Kernel is itself a module — foundational, always loaded, composed first
`KernelModule : IModule` (in `Kernel.Infrastructure`, exactly where each feature module keeps its `XModule`)
registers the generic repositories, Identity, the current‑user accessor and the kernel seeder; it declares
`IsFoundational => true`, so it always loads (ignoring the module‑selection config) and its model composes
before the feature modules that FK to its entities. The kernel is four Clean‑Architecture layers
(`Domain/Application/Infrastructure/Api`) and holds only
cross‑cutting concerns + shared reference data. *Rejected:* a special, non‑module "infrastructure" the
host wires by hand — treating the kernel as just another (foundational) module removes that special case.

### D5 — Modules are discovered dynamically and selected by configuration
The kernel's `AddModules(IConfiguration)` scans the app's own `ModularShop.*` assemblies for `IModule`
implementations, keeps the ones named in the `appsettings.json` `"Modules"` array (**absent ⇒ all**; the
foundational kernel always), orders them foundational‑first, and registers each. `Program.cs` is a single
`AddModules(...)` call; there is no hand‑maintained module list. Adding a feature = create the module and
reference it; a micro‑solution references only the modules it wants (and generates its own migration for
that set). *Rejected:* an explicit `HostModules.All()` list (a second place to maintain, prone to drift)
and Platform's heavier registry/loader with four overlapping discovery paths.

### D6 — One host `DbContext`, composed from each module's real `DbContext` by reflection
There is a single runtime context, `ModularShopDbContext`, holding no entities of its own. Each module —
the kernel included — owns an **ordinary** `DbContext` that configures its entities and their schema in
its own `OnModelCreating`. The host's `ApplyModuleModels` extension instantiates each module context with
throwaway options (never connected) and invokes its **protected** `OnModelCreating` by reflection onto the
one shared `ModelBuilder`. So a module context is "just a `DbContext`" — no base class or marker interface
— yet services, transactions and migrations all deal with a single context. *Rejected:* a real context
per module at runtime (cross‑context transactions, "which context do I inject?"); and the earlier
`IModuleModel`/`IModelContributor` shims (unnecessary once reflection reaches the protected method).

### D7 — Schema‑per‑module; one centralised, factory‑less migration chain
One database, one schema per module; each module places its own tables via `ToTable(name, schema)`, and
the kernel's `HasDefaultSchema("kernel")` catches the Identity + shared entities. The host owns **one**
migration chain (`ModularShop.Infrastructure/Migrations`); `dotnet ef` builds the context from the app's own
service provider (no design‑time factory), so the migration reflects the config‑selected module set.
*Rejected:* per‑module migration chains (ordering and history headaches for a single database).

### D8 — Our own repository layer + `UnitOfWork`; the Application layer stays EF‑free
Use cases depend on `IReadRepository<T>` / `IRepository<T>` (Kernel.Domain) + `IUnitOfWork`
(Kernel.Application); a single open‑generic `Repository<T>` over the one host context serves every
module. Reads are materialised + async, NoTracking by default; `SaveChanges` is the unit of work's job.
**Repositories return entities only** — a non‑entity read shape (Support's ticket‑list count projection)
is a dedicated `ITicketSummaryQuery`, not a repository method. *Rejected:* injecting `DbContext` into the
Application layer (leaks EF into the core); `Ardalis.Specification` (unneeded); returning projections from
repositories (blurs the entity boundary).

### D9 — EF‑generated `Guid` keys
`Entity` no longer assigns its own key; EF generates sequential `Guid`s (`ValueGeneratedOnAdd`). Aggregate
constructors take an **optional** trailing `id` so seeds can pass fixed Guids while runtime code omits it.
*Rejected:* client‑assigned keys — they caused a tracked‑parent child‑insert concurrency bug that
EF‑generated keys fix.

### D10 — Shared kernel entities: `Customer` and `Currency`
The two reference entities used by more than one module live in the kernel; modules link to them with real
cross‑schema FKs, which is the deliberate signal "this is shared kernel data" (versus another module's
private data, reached only via a contract). *Rejected:* a copy per module (drifts out of sync) or a
"customers module" (everything would depend on it).

### D11 — ASP.NET Core Identity in the kernel; cookie auth; the whole app gated
`KernelDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`; `AuthController` does
cookie register/login/logout/me; `ApiControllerBase` carries `[Authorize]`, so every module endpoint
needs a signed‑in user, read only through the kernel's `ICurrentUser`. The identity **entities**
(`ApplicationUser`/`ApplicationRole`) live in `Kernel.Domain` (the core) — a documented Clean‑Arch exception
so no layer needs the kernel's Infrastructure to name the user; only the Identity **stores** stay in
`Kernel.Infrastructure` (see D17). *Rejected:* a header‑based fake user (doesn't demonstrate a real
cross‑cutting concern); per‑module auth (auth is cross‑cutting).

### D12 — Two explicit inter‑module styles: a sync interface and an async event
Synchronous request/response goes through a module's public interface (`IWarehouseApi`, returning DTOs);
"something happened" goes through an integration event (`OrderPlaced`, a MediatR `INotification` in
`Sales.Contracts`) handled independently by Warehouse and Shipping. MediatR is registered **per module**
that uses it. *Rejected:* modules reading each other's tables (the cardinal MM sin); a hand‑rolled bus
(MediatR is already a dependency and does it cleanly). *Next step:* a transactional outbox behind the
same publish call.

### D13 — Use cases as `UseCase` classes (convention‑registered); each module registers its own controllers
Each operation is one `UseCase` subclass, registered by scanning the module's Application assembly
(`AddUseCases`) — no hand‑listing. **Controllers** live in each module's `Api` project (referenced by its
`Infrastructure`), and each module registers its own in its `IModule.Register` as an MVC application part:
`services.AddControllers().AddApplicationPart(typeof(SomeController).Assembly)`. The host turns **off** the
Web SDK's implicit controller discovery (`GenerateMvcApplicationPartsAssemblyAttributes=false` in
`ModularShop.Server.csproj`), so a module ships its controllers only by adding its own `Api` assembly — which
makes every `*.Infrastructure → *.Api` reference a *real, used* one (a "remove unused references" pass can no
longer silently delete it and 404 a whole module) and means a module the config didn't select contributes no
routes. *Rejected:* the SDK's **implicit** discovery we used before (a referenced `*.Api`'s controllers
appeared automatically) — its `*.Infrastructure → *.Api` reference was an invisible, unused side‑effect,
prunable and thus fragile, and it exposed routes even for *deselected* modules; a **host‑side** central
`AddApplicationPart` list (a second place to maintain — each module registering its own part keeps composition
in the module, per D5); and a CQRS command/query bus (out of scope by the brief).

### D14 — `Ardalis.Result` + `ApiResponse<T>`; USD‑only demo; Central Package Management; Swagger
Every use case returns `Ardalis.Result<T>`, mapped to an `ApiResponse<T>` envelope + HTTP status. The demo
prices everything in **USD** (Currency stays a shared entity, but one currency keeps the seed simple).
NuGet versions are centralised in `Directory.Packages.props`. Swagger is enabled in every environment.
*Rejected:* a hand‑rolled `Result`/envelope (Ardalis is tiny and standard); mixed currencies (added noise
without teaching value).

### D15 — Frontend: Vite + React + TypeScript, pnpm, feature folders, cookie‑auth gating
A single SPA with `features/{catalog,orders,shipments,support}` mirroring the modules, an `AuthContext`
gate, and `credentials:'include'`; it builds into `Server/wwwroot` so the host serves it same‑origin.
*Rejected:* Module Federation / micro‑frontends (Platform has them; far beyond a teaching example).

### D16 — A thin web host + a dedicated `ModularShop.Infrastructure` composition/persistence layer
`ModularShop.Server` is only the **web** host: `Program.cs` wires the HTTP pipeline and points the one
`DbContext` at its connection. The host `ModularShopDbContext` and the centralised migration chain live in a
separate **`ModularShop.Infrastructure`** project, which references the kernel's Infrastructure plus every
feature module's Infrastructure — so it is the single place that knows the concrete module set, and the web
host stays free of persistence detail (it references just `ModularShop.Infrastructure` + `Kernel.Api`).
*Rejected:* keeping the host context + migrations inside `ModularShop.Server` (mixes web composition with
persistence and makes the web project the migrations assembly).

### D17 — `Kernel.Infrastructure` is an implementation detail; only module Infrastructure (and the host) reference it
The kernel is layered like a feature module, so its `IModule` implementation (`KernelModule`) and its
`ICurrentUser` adapter (`CurrentUser`) live in **`Kernel.Infrastructure`**, and its Web layer was renamed
**`Kernel.Web` → `Kernel.Api`** to hold controllers only (`AuthController`, `ApiResponse`,
`ApiControllerBase`, exception middleware). With the identity **entities** also moved to `Kernel.Domain`
(D11), nothing outside a module's own `*.Infrastructure` (and the composition‑root host) references
`Kernel.Infrastructure` — the kernel's Api layer and every module's non‑Infrastructure layers no longer can.
*Rejected:* the kernel's `IModule`/current‑user living in its Web/Api layer (forces the Api layer to depend
on Infrastructure — the exact coupling this removes) and a dead `AddKernelWeb()` extension (deleted; its
registrations already live in `KernelModule`).

### D18 — A module declares its cross‑module runtime needs via `RequiredModules`; the host validates the selection at startup
Once selection is config‑driven (D5), a deployment can pick an **incomplete** set — e.g. `"Modules": ["Sales"]`
without Warehouse, even though Sales calls `IWarehouseApi` synchronously to place an order. A module now
declares such needs on the `IModule` contract itself — `IReadOnlyCollection<string> RequiredModules` (default
none). Only `SalesModule` needs one today — `["Warehouse"]`; Warehouse and Shipping merely *react* to Sales'
`OrderPlaced` event and run fine without it, so they declare nothing. `ModuleRegistration.AddModules` runs `ValidateRequiredModules`
right after selection and **before** any registration or migration, throwing a single aggregated
`InvalidOperationException` ("Module 'Sales' requires module 'Warehouse', but 'Warehouse' is not enabled.")
when the set is incomplete — a clear boot‑time failure instead of a DI resolution error on the first order.
This lives in the **kernel** so it protects every host identically: the demo, and any packaged
micro‑solution. *Rejected:* encoding the requirement in a **package graph** (a module's package dragging in
the whole module it depends on) — that couples runtime topology to packaging, forces a heavier install than
some clients want, and would still not stop a client from *deselecting* the dependency in config. Keeping the
rule in `RequiredModules` makes it the **single source of truth**, independent of how the DLLs arrive (project
reference or NuGet package). See `packaging-and-distribution.md` §3.4.

**Explicitly out of scope** (all legitimate in production, none needed to teach MM): CQRS buses, test
projects, a transactional outbox/inbox, Docker, multi‑tenancy, SignalR, background jobs, and real SSO —
represented where relevant by lightweight seams (`ICurrentUser`, `ILogger`, middleware).

---

## 3. Evolution (superseded ideas, one line each)

The design was pushed through several iterations before reaching the shape above; these are the notable
reversals, kept only as pointers to *why* today's design looks the way it does:

- Hand‑rolled `Result`/envelope → **`Ardalis.Result`** (kept the `ApiResponse<T>` envelope).
- Hand‑rolled in‑process event bus → **MediatR** `INotification`s (registered per module).
- Minimal APIs → **real controllers** invoking use cases.
- SDK‑implicit controller discovery (a referenced `*.Api`'s controllers auto‑registered) → **each module
  registers its own controllers** via `AddControllers().AddApplicationPart(...)` in `Register`, with the
  host's implicit discovery turned off (D13).
- A real `DbContext` per module at runtime → **one host context** composing every module.
- Per‑module *blueprint* contexts + `IModuleModel`/`ModuleModelBuilder` (reflecting `DbSet`s) → each
  module's **real `DbContext`**, its own `OnModelCreating` reflected by the host.
- `IModelContributor` + `ModuleDbContext` shim/base → **plain `DbContext` + `ApplyModuleModels`** (the
  reflection reaches the protected method directly).
- `DbContext` injected into the Application layer → **our own repositories + `UnitOfWork`**;
  `Ardalis.Specification` was briefly adopted, then removed.
- `ITicketRepository` returning a projection → **`ITicketSummaryQuery`** (repositories return entities only).
- Client‑assigned `Guid` keys (`ValueGeneratedNever`) → **EF‑generated keys**.
- Hand‑maintained `HostModules.All()` list + a design‑time factory → **dynamic `AddModules` discovery**
  (config‑selected) and **factory‑less** migrations.
- Host `DbContext` + migrations inside `ModularShop.Server` → a dedicated **`ModularShop.Infrastructure`** layer (D16).
- `Kernel.Web` (holding `KernelModule` + `CurrentUser`) → **`Kernel.Api`** (controllers only); the kernel's `IModule` + current‑user adapter moved into `Kernel.Infrastructure` (D17).
- `ApplicationUser`/`ApplicationRole` in `Kernel.Infrastructure` → **`Kernel.Domain`** (identity entities in the core; a documented exception).
- Inline migrate‑then‑seed block in `Program.cs` → **`ModuleRegistration.InitializeModulesAsync`**.
- String Identity keys → **`Guid`** throughout.
- Node 20 / pnpm 9 → **Node 24 / pnpm 11**.

---

See `architecture.md` for the concepts + diagrams, and `platform-mapping.md` for how this maps back to the
Platform solution.
