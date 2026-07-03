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
| `IModule` + `BaseModule` + capability interfaces (`IModuleWithDbContext`, `IModuleWithFrontend`, `IModuleWithPermissions`, `IModuleEntityProvider`) | **Adopted, simplified** | A small `IModule` (`Register`) for services, plus an **`IModuleModel`** that directly mirrors Platform’s **`IModuleEntityProvider`** — each module contributes its entities to the one host context. Controllers are discovered via MVC `ApplicationPart`s (no `MapEndpoints`). Dropped the other capability interfaces. |
| `ModuleRegistry` + `ModuleLoader` (reflection discovery, topological dependency sort, `ApplicationPartManager`) | **Adapted → simpler** | Explicit `IReadOnlyList<IModule>` in `HostModules`; each module’s Api assembly added as an `ApplicationPart`. Reflection discovery is powerful but hides the wiring; an explicit list is clearer for teaching. |
| `IRepository<T>` + generic `Repository<T>` (reads default to `NoTracking`) | **Adopted (hand‑rolled)** | Our own `IReadRepository<T>`/`IRepository<T>` (Kernel.Domain) over a single open‑generic `Repository<T>` bound to the one host context (Kernel.Infrastructure), plus `IUnitOfWork` for `SaveChanges`. Reads are materialised + `NoTracking`; by‑id / for‑update loads are tracked; includes are typed *and* string (cross‑cutting). A module adds a **specific** repository only when the generic one isn't enough — Support's `ITicketRepository` (see decision‑log D30). |
| `Ardalis.Result` + `ApiResponse<T>` envelope | **Adopted (Ardalis package)** | Every use case returns `Ardalis.Result<T>`; the kernel base controller maps it to the kept `ApiResponse<T>` envelope + HTTP status. 1:1 with Platform. |
| Clean‑Architecture shared layer (`TNEX.Core`/`Application`/`Infrastructure`/`Api`) | **Adopted as the Kernel** | `Kernel.Domain` / `.Application` / `.Infrastructure` / `.Web` — a four‑layer kernel. Home of Identity, the shared entities, logging. |
| MVC controllers + `Swagger` | **Adopted** | Real controllers (in each module’s `*.Api`) invoke use cases and return `ApiResponse`; Swashbuckle serves Swagger at `/swagger`. |
| Per‑module `DbContext` + schema | **Adopted as *blueprints*** | Each module keeps a `DbContext` for organisation, but a single **host context absorbs them all** (reflecting each module’s DbSets) — the shape of Platform’s `MainDbContext` + `IModuleEntityProvider`. Schema‑per‑module is preserved (assigned per entity); migrations are **centralised** in the host. |
| `MainDbContext` composing module entities via `IModuleEntityProvider`; `ModularDbContext<T>` + `AddModule<T>()` | **Adopted (the core idea)** | This *is* the design here: `ModularShopDbContext` composes exactly the registered modules via `IModuleModel`. Platform’s per‑customer `ConfigureFieldExclusions()` (column‑level blacklists) is the one part left out — not needed for the teaching goal. |
| Shared **domain entities** in `TNEX.Core` (Contracts, Partners, Devices, ValueRecords, …) | **Adopted — kept minimal** | Only genuinely shared *reference* entities go in the kernel: `Customer` (Sales/Shipping/Support) and `Currency` (Warehouse/Sales), linked by cross‑schema FK. A module’s *own* business entities stay in the module, so the kernel stays lean. |
| ASP.NET Identity in `TNEX.Core`/`Infrastructure` (`ApplicationUser`, a `CoreDbContext : IdentityDbContext`) | **Adopted** | `ApplicationUser`/`ApplicationRole` + `KernelDbContext : IdentityDbContext` in the kernel; a cookie `AuthController`; every endpoint `[Authorize]`. Mirrors Platform’s Identity‑in‑the‑core approach. |
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
| `TNEX.Api` (`ApiResponse`, middleware, controllers host bits) | `ModularShop.Kernel.Web` + `ModularShop.Server` |
| `TNEX.Server` (host entry point) | `ModularShop.Server` (composition root) |
| `MainDbContext` composing modules via `IModuleEntityProvider` | `ModularShop.Server/Persistence/ModularShopDbContext.cs` (composes via `IModuleModel`) |
| `TNEX.Module.*` (feature modules) | `ModularShop.Modules.{Sales,Warehouse,Shipping,Support}.{Domain,Application,Infrastructure,Api}` (+ `*.Contracts` where needed) |
| `TNEX.Core/Services/Module/IModule*` + `IModuleEntityProvider` | `Kernel.Infrastructure/IModule.cs` + `Kernel.Infrastructure/Persistence/IModuleModel.cs` |
| `TNEX.Core/Services/Repository/IRepository` + `Repository<T>` | `Kernel.Domain/Repositories/{IReadRepository,IRepository}` + `Kernel.Infrastructure/Persistence/Repositories/{ReadRepository,Repository}`; `IUnitOfWork` in `Kernel.Application`, `UnitOfWork` in `Kernel.Infrastructure` |
| `Ardalis.Result` + `TNEX.Api/.../ApiResponse` | `Ardalis.Result` (NuGet) + `Kernel.Web/ApiResponse.cs` |
| ASP.NET Identity (`ApplicationUser`, Identity in Core/Infrastructure) | `Kernel.Infrastructure/Identity/*` + `KernelDbContext : IdentityDbContext` + `Kernel.Web/AuthController.cs` |

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

1. **One module contract, one loading mechanism.** Consolidate on a single `IModule` (like the one
   here). Route **every** module — including `ProjectManagement` — through the same registration list
   or config. Remove hard‑coding from `Program.cs`.

2. **Make data ownership a hard rule: one *blueprint* context per module, composed into one host
   context, with centralised migrations.** Keep `MainDbContext` as the single runtime context, but make
   **every** module contribute its slice through one uniform `IModuleEntityProvider`/`IModuleModel` (as
   demonstrated here) — including `ImportManager` and `Compliance`, whose tables are currently
   hand‑composed. Assign each entity its module’s schema, and let the host own **one** migration chain.
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
