# Platform mapping — what we took from `./Platform`, what we left, and how to finish the transition

This example was built after inspecting the real **`./Platform`** solution (`TNEX.sln`, ~22 projects),
which is **mid‑transition** from a 6‑project Clean Architecture (TNEX.API / TNEX.Application /
TNEX.Configuration / TNEX.Core / TNEX.Infrastructure / TNEX.Server) to a Modular Monolith (the newer
`TNEX.Module.*` projects: AiReporting, ImportManager, Controlling, Notification, Tasks, Compliance,
FraudDetection, ProjectManagement, Allocation).

The goal here was **not** to copy Platform, but to adopt the parts that genuinely improve a small,
correct MM and leave the rest. Every decision below was judged on: does it improve **simplicity,
correctness, maintainability, and educational value**?

---

## 1. Components inspected and the verdict

| Platform component | Verdict | How it appears here (or why not) |
|---|---|---|
| `IModule` + `BaseModule` + capability interfaces (`IModuleWithDbContext`, `IModuleWithFrontend`, `IModuleWithPermissions`, `IModuleEntityProvider`) | **Adopted, simplified** | A single small `IModule` (`Name`, `ContextType`, `IsFoundational`, `RequiredModules`, `Register`) — no `BaseModule`, no capability‑interface family. A module contributes its slice of the model through its **own `DbContext`** (the host reflects each context's `OnModelCreating`), not a separate provider interface. Each module registers **its own controllers** as an MVC application part in `Register` (`AddControllers().AddApplicationPart(...)`); the host turns the SDK's implicit discovery off, so it registers none itself (decision‑log D13). |
| `ModuleRegistry` + `ModuleLoader` (reflection discovery, topological dependency sort, `ApplicationPartManager`) | **Adopted → drastically simplified** | One kernel extension, `AddModules`: it scans the app's own `ModularShop.*` assemblies for `IModule` types, selects which to activate from an `appsettings.json` `"Modules"` array (absent ⇒ all; the foundational kernel always loads), and registers them foundational‑first. No stateful registry/loader, no topological sort, no manifest files, no `ApplicationPartManager` juggling — **one** discovery path instead of Platform's four. |
| `IRepository<T>` + generic `Repository<T>` (reads default to `NoTracking`) | **Adopted (hand‑rolled)** | Our own `IReadRepository<T>`/`IRepository<T>` (Kernel.Domain) over a single open‑generic `Repository<T>` bound to the one host context (Kernel.Infrastructure), plus `IUnitOfWork` for `SaveChanges`. Reads are materialised + `NoTracking`; by‑id / for‑update loads are tracked; includes are typed (compile‑time‑safe). A module adds a **specific** repository only when the generic one isn't enough — it still returns entities. A non‑entity read shape (e.g. Support's ticket‑list count projection) is a separate read‑only query object, not a repository (`ITicketSummaryQuery`; see decision‑log D8). |
| `Ardalis.Result` + `ApiResponse<T>` envelope | **Adopted (Ardalis package)** | Every use case returns `Ardalis.Result<T>`; the kernel base controller maps it to the kept `ApiResponse<T>` envelope + HTTP status. 1:1 with Platform. |
| Clean‑Architecture shared layer (`TNEX.Core`/`Application`/`Infrastructure`/`Api`) | **Adopted as the Kernel** | `Kernel.Domain` / `.Application` / `.Infrastructure` / `.Api` — a four‑layer kernel. Home of Identity, the shared entities, logging. |
| MVC controllers + `Swagger` | **Adopted** | Real controllers (in each module’s `*.Api`) invoke use cases and return `ApiResponse`; Swashbuckle serves Swagger at `/swagger`. |
| Per‑module `DbContext` + schema | **Adopted (real contexts, composed)** | Each module owns an ordinary `DbContext` that configures its entities *and* their schema; a single **host context composes them all** by invoking each context's `OnModelCreating` via reflection — the shape of Platform’s `MainDbContext` + `IModuleEntityProvider`, minus the provider interface. Schema‑per‑module is preserved (each module calls `ToTable(name, schema)`); migrations are **centralised** in the host. |
| `MainDbContext` composing module entities via `IModuleEntityProvider`; `ModularDbContext<T>` + `AddModule<T>()` | **Adopted (the core idea)** | This *is* the design here: `ModularShopDbContext` composes exactly the registered modules via `ApplyModuleModels` (reflection). Platform’s per‑customer `ConfigureFieldExclusions()` (column‑level blacklists) is the one part left out — not needed for the teaching goal. |
| Shared **domain entities** in `TNEX.Core` (Contracts, Partners, Devices, ValueRecords, …) | **Adopted — kept minimal** | Only genuinely shared *reference* entities go in the kernel: `Customer` (Sales/Shipping/Support) and `Currency` (Warehouse/Sales), linked by cross‑schema FK. A module’s *own* business entities stay in the module, so the kernel stays lean. |
| ASP.NET Identity in `TNEX.Core`/`Infrastructure` (`ApplicationUser`, a `CoreDbContext : IdentityDbContext`) | **Adopted** | `ApplicationUser`/`ApplicationRole` entities in `Kernel.Domain` (the core), `KernelDbContext : IdentityDbContext` + the Identity stores in `Kernel.Infrastructure`, a cookie `AuthController` in `Kernel.Api`; every endpoint `[Authorize]`. Mirrors Platform’s Identity‑in‑the‑core approach — the entities sit in the core exactly as in `TNEX.Core`. |
| Inter‑module communication (Platform: shared entities in `TNEX.Core`, direct DI, occasional `IModuleRegistry` lookup; **no event bus**) | **Replaced with an explicit model** | Two first‑class styles: a public interface (`IWarehouseApi`, sync) and integration events (`OrderPlaced` as a **MediatR `INotification`**, async). This is an **improvement** over Platform’s ad‑hoc coupling. |
| SignalR hubs (`NotificationHub`, `ImportHub`, …) | **Left out** | Not needed for the order→shipment flow. |
| `AiReporting` module (LLM pipeline, RAG, tool orchestration) | **Left out** | Domain‑specific; adds several SDKs and dozens of files; distracts from MM fundamentals. |
| `ProjectManagement` hard‑coded in `Program.cs` | **Left out (and called out as an anti‑pattern)** | Contradicts "modules are opt‑in". Here all modules go through the same list. |
| `Compliance` (declares a `ComplianceDbContext` but its tables are hard‑coded into `MainDbContext`) | **Left out (tech debt)** | Ambiguous ownership. Our rule: a module owns its context *and* its tables, or it doesn’t — never half. |
| `ImportManager` (declares `ImportDbContext` but owns **no** migrations; tables created by whoever composes it) | **Left out (inconsistent)** | Confusing schema ownership. Here every module owns its own migrations. |
| Module Federation frontend / Roslyn plugin compilation / signing | **Left out** | Enterprise plug‑in machinery; far beyond a teaching example. Our React app is one SPA with feature folders mirroring the modules. |
| Multi‑tenancy, temporal tables, ForgeRock/Azure AD SSO, Hangfire, OpenTelemetry, Elasticsearch/Serilog sinks, Mapster, `FixedListTypes` | **Left out** | All legitimate in production; none is needed to teach MM. Identity/logging are represented by lightweight seams (`ICurrentUser`, `ILogger`, middleware). |
| PostgreSQL variant (`MSDekaControlling`) | **Left out** | The brief fixes the database to MSSQL. |

The full reasoning for each of our own choices is in [`decision-log.md`](./decision-log.md).

---

## 2. Project mapping (Platform → ModularShop)

| Platform | ModularShop equivalent |
|---|---|
| `TNEX.Core` (interfaces, base types, contracts) | `ModularShop.Kernel.Domain` + `ModularShop.Kernel.Application` |
| `TNEX.Infrastructure` (EF, repositories, module machinery) | `ModularShop.Kernel.Infrastructure` |
| `TNEX.Api` (`ApiResponse`, middleware, controllers host bits) | `ModularShop.Kernel.Api` + `ModularShop.Server` |
| `TNEX.Server` (host entry point) | `ModularShop.Server` (web host) + `ModularShop.Infrastructure` (composition/persistence) |
| `MainDbContext` composing modules via `IModuleEntityProvider` | `ModularShop.Infrastructure/Persistence/ModularShopDbContext.cs` (composes via `ApplyModuleModels` reflection) |
| `TNEX.Module.*` (feature modules) | `ModularShop.Modules.{Sales,Warehouse,Shipping,Support}.{Domain,Application,Infrastructure,Api}` (+ `*.Contracts` where needed) |
| `TNEX.Core/Services/Module/IModule*` + `IModuleEntityProvider` | `Kernel.Infrastructure/IModule.cs` + `ModuleRegistration.cs` (discovery/selection) + `Persistence/ModuleModelComposition.cs` (reflection) |
| `TNEX.Core/Services/Repository/IRepository` + `Repository<T>` | `Kernel.Domain/Repositories/{IReadRepository,IRepository}` + `Kernel.Infrastructure/Persistence/Repositories/{ReadRepository,Repository}`; `IUnitOfWork` in `Kernel.Application`, `UnitOfWork` in `Kernel.Infrastructure` |
| `Ardalis.Result` + `TNEX.Api/.../ApiResponse` | `Ardalis.Result` (NuGet) + `Kernel.Api/ApiResponse.cs` |
| ASP.NET Identity (`ApplicationUser`, Identity in Core/Infrastructure) | `Kernel.Domain/Identity/*` (entities) + `KernelDbContext : IdentityDbContext` & the Identity stores (`Kernel.Infrastructure`) + `Kernel.Api/AuthController.cs` |

---

## 3. Where Platform is not‑yet‑correct MM (honest assessment)

From the inspection, the transition is genuinely incomplete in a few consistent ways:

1. **Inconsistent data ownership.** Some modules own a `DbContext`+migrations (AiReporting,
   ProjectManagement); others declare a context but don’t own their tables/migrations (ImportManager,
   Compliance), which are composed into `MainDbContext`. There is no single rule.
2. **No formal inter‑module communication.** Modules mostly couple through shared entities in
   `TNEX.Core` and direct DI; there is no event bus and no per‑module public contract. This blurs
   boundaries — a module can reach a lot of another module’s world through shared Core types.
3. **The "shared kernel" is too big.** `TNEX.Core`/`Application` hold a great deal of *business* domain
   (Contracts, Partners, Devices, ValueRecords…), not just cross‑cutting primitives. That shared
   business model is what actually couples the modules.
4. **Host knows too much.** `ProjectManagement` is hard‑coded in `Program.cs`; module loading mixes
   hard‑coding, config, and reflection.
5. **Encapsulation not enforced.** Module internals are largely `public`; nothing prevents one module
   referencing another’s implementation.

None of this makes Platform "wrong" — it’s a real system mid‑migration — but these are the gaps between
it and a textbook MM.

---

## 4. Incremental transition plan (half‑finished → correct MM)

Do these in order; each step is shippable on its own and keeps the app working.

1. **One module contract, one loading mechanism.** Consolidate on a single `IModule` and one
   discovery+selection path (like `AddModules` here: scan for `IModule` types, choose the active set
   from config). Route **every** module — including `ProjectManagement` — through it, and remove the
   hard‑coding from `Program.cs`.

2. **Make data ownership a hard rule: one `DbContext` per module, composed into one host context, with
   centralised migrations.** Keep `MainDbContext` as the single runtime context, but make **every**
   module contribute its slice through its own `DbContext` — the host reflecting each context's
   `OnModelCreating` (as demonstrated here) — including `ImportManager` and `Compliance`, whose tables
   are currently hand‑composed. Let each module place its own tables in its schema, and let the host own
   **one** migration chain.
   Target: *exactly one declared owner per table*, even though a single context persists them all. (This
   is the biggest and most valuable step, and it matches where `MainDbContext` is already heading.)

3. **Introduce per‑module public contracts.** For each module, create a `*.Contracts` assembly holding
   only the interfaces/DTOs/events other modules may use. Move everything else to `internal`. Enforce
   with project references, then add a **NetArchTest** test that fails the build on a boundary
   violation.

4. **Replace ad‑hoc coupling with explicit communication.** Add an in‑process event bus (**MediatR**,
   as demonstrated here). Where a module needs data *now*, expose a use‑case interface in its
   `*.Contracts` (sync). Where a module reacts to something that *happened*, publish an integration
   event — a MediatR `INotification` — and handle it with `INotificationHandler<T>` (async). Migrate
   the current shared‑entity/`IModuleRegistry` coupling onto these two channels one interaction at a
   time.

5. **Shrink the shared kernel.** Keep only cross‑cutting code in `TNEX.Core`/`Infrastructure`/`Api`
   (identity, logging, `Result`/`ApiResponse`, base types, the module/event‑bus infra). Push each
   business concept (Contracts, Partners, Devices, ValueRecords…) **into the module that owns it**;
   expose the rest as contracts. This is what actually decouples the modules.

6. **Centralise migration; keep seeding per module.** The host migrates the one context once at startup;
   each module keeps an `IModuleInitializer` that only *seeds* its own tables — ordered so shared kernel
   data (customers, currencies, users) exists first — as demonstrated here.

7. **Harden for production (later, only if needed).** Add a transactional **outbox/inbox** behind the
   MediatR publish for reliable events; consider **database‑per‑module** for any module that must scale
   or be extracted independently; keep real auth (Azure AD/ForgeRock) behind `ICurrentUser`.

The end state is exactly the shape ModularShop demonstrates — just at Platform’s scale.
