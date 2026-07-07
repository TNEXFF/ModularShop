# ModularShop — a Modular Monolith you can read in an afternoon

A small, **correct**, runnable **Modular Monolith (MM)** in **ASP.NET Core (.NET 10) + React**, on
**MSSQL**. It exists to teach the MM architecture by example and to serve as the *target design* for
migrating the existing `../Platform` solution.

Four modules — **Sales**, **Warehouse**, **Shipping**, and the deliberately‑independent **Support** —
run **in one process, deploy as one unit, and share one database with a schema each**, on top of a lean
shared **Kernel** (Identity + the shared `Customer`/`Currency` entities + cross‑cutting code). One
realistic flow (place an order → check price/stock → decrement stock → create a shipment) demonstrates
**both** ways modules talk to each other. Every module is a small **Clean Architecture** stack of its
own (Domain · Application · Infrastructure · Api).

> **The defining choice:** each module owns an ordinary `DbContext`. At runtime there is **one host
> context** (`ModularShopDbContext`) that harvests every module’s model (invoking its `OnModelCreating`
> by reflection), owns the **centralised** migrations, and is the only context services touch. The
> **kernel is itself a module**, and modules are **discovered dynamically** and chosen by configuration.
> See [`docs/architecture.md`](docs/architecture.md) §3 and §5.

> New here? Read [`docs/architecture.md`](docs/architecture.md) for the concepts + diagrams,
> [`docs/decision-log.md`](docs/decision-log.md) for *why* every choice was made, and
> [`docs/platform-mapping.md`](docs/platform-mapping.md) for how this maps back to `../Platform`.

---

## What is a Modular Monolith?

A **single deployable unit** whose internals are split into loosely‑coupled, highly‑cohesive **modules
organised by business capability** (not by technical layer). Each module owns its data and exposes a
small public contract; everything else is hidden. You get **microservice‑style boundaries** (high
cohesion, low coupling, data ownership) with **monolith simplicity** (in‑process calls, one database,
single deployment, easy debugging).

### When to use it
- Clean, enforceable boundaries **without** the operational cost of microservices.
- Early‑stage / evolving products where the right service boundaries aren’t clear yet — *start modular,
  extract services later only if you must.*
- Untangling a "big ball of mud" with a safe, incremental path to strong modularity.

### When **not** to
- You genuinely need independent deployment/scaling per capability → consider microservices (and pay
  their premium deliberately).

---

## Solution layout

Every **module** is split into one project **per Clean‑Architecture layer** (Domain, Application,
Infrastructure, Api), plus a tiny `*.Contracts` project *only where another module needs to look*. The
**Kernel** is layered the same way.

```
ModularShop/
├─ ModularShop.slnx                      # solution (modern .slnx format; CLI or a recent VS/Rider)
├─ docs/  architecture.md · decision-log.md · platform-mapping.md
├─ src/
│  ├─ ModularShop.Server/                # HOST = web composition root (Program.cs). No business logic.
│  │  └─ appsettings.json                #   optional "Modules": ["Sales",…] selects which modules load
│  ├─ ModularShop.Infrastructure/        # host persistence layer: ModularShopDbContext (the ONE runtime
│  │                                     #   context) + Migrations/ (the ONE centralised migration chain)
│  ├─ Kernel/                            # shared kernel — itself Clean Architecture, kept lean
│  │  ├─ ModularShop.Kernel.Domain            # Entity · Customer · Currency · ApplicationUser/Role (identity)
│  │  ├─ ModularShop.Kernel.Application       # ICurrentUser · UseCase base · IUnitOfWork
│  │  ├─ ModularShop.Kernel.Infrastructure    # KernelDbContext(Identity), KernelModule + AddModules discovery, reflection composition, Repository<T>, CurrentUser, seeder
│  │  └─ ModularShop.Kernel.Api               # ApiResponse, ApiControllerBase([Authorize]), AuthController, middleware
│  └─ Modules/
│     ├─ Sales/      .Domain·.Application·.Infrastructure·.Api  (+ .Contracts: OrderPlaced)
│     ├─ Warehouse/  .Domain·.Application·.Infrastructure·.Api  (+ .Contracts: IWarehouseApi)
│     ├─ Shipping/   .Domain·.Application·.Infrastructure·.Api  (no .Contracts — nothing calls it)
│     └─ Support/    .Domain·.Application·.Infrastructure·.Api  (independent — no .Contracts, no events)
└─ client/                               # React + TypeScript SPA (Vite, pnpm); builds into Server/wwwroot
```

Each `*.Infrastructure` holds the module’s own `DbContext` (which configures its entities + schema) under
`Persistence/`. There are **no** per‑module migrations — the host owns the single chain in `ModularShop.Infrastructure/Migrations`.

| Layer (per module) | Contents | May reference |
|---|---|---|
| `*.Domain` | Entities, enums, domain logic | Kernel.Domain |
| `*.Application` | Use cases (inject `IReadRepository<T>`/`IRepository<T>` + `IUnitOfWork`), DTOs, mappings | Domain, Kernel.Domain, Kernel.Application, other modules’ `*.Contracts` (**no EF Core**) |
| `*.Infrastructure` | The module's `DbContext`, `XModule` (its `IModule`), seeding, event handlers | Application, Domain, Kernel.Infrastructure |
| `*.Api` | Controllers only (invoke use cases, return `ApiResponse`) | Application, Kernel.Api |
| `*.Contracts` | Public surface other modules may use | nothing (or `MediatR.Contracts`) |

**Boundary rule (compile‑time):** a module’s projects reference **only** the Kernel and other modules’
`*.Contracts`. Support references only the Kernel. The host (`ModularShop.Server` + `ModularShop.Infrastructure`)
is the only place that references every module. **`Kernel.Infrastructure` is a kernel implementation detail:
only the modules’ own `*.Infrastructure` projects (and the host) reference it — the kernel’s `Api` layer and
every module’s other layers never do.**

**Selecting which modules load.** The kernel’s `AddModules` discovers every referenced module and, with
no configuration, loads them all. A deployment can pick a subset in `appsettings.json` — the kernel
always loads regardless:

```jsonc
"Modules": [ "Sales", "Support" ]   // load Kernel (always) + Sales + Support; omit the key → load all
```

A new **micro‑solution** is then a fresh host that references the kernel + the modules it wants, calls
`AddModules`, and generates its own migration for that set (see [`docs/architecture.md`](docs/architecture.md) §5).

---

## The demo domain & flow

The whole app sits behind a **login** (authentication is a kernel concern; every endpoint is
`[Authorize]`). Once signed in, placing an order exercises the two inter‑module styles in one request:

1. **Synchronous, via a public interface** — Sales calls `IWarehouseApi.GetProductsAsync(...)` for the
   current **price + stock**. Sales never sees Warehouse’s tables.
2. **Asynchronous, via an integration event** — Sales saves the order, then publishes `OrderPlaced`
   (a MediatR `INotification`). **Warehouse** decrements stock and **Shipping** creates a `Pending`
   shipment — each reacting independently. All three run on the **one** host context.

Requests flow **Controller → Use case → (repositories + `IUnitOfWork` | `IWarehouseApi` | MediatR)**.
Controllers hold no logic; they invoke a single use case and wrap its `Ardalis.Result` in an `ApiResponse<T>`.

### API endpoints
| Method & path | Module | Purpose |
|---|---|---|
| `POST /api/auth/register` · `login` · `logout` · `GET /api/auth/me` | Kernel | authentication (cookie) |
| `GET /api` | host | app info + loaded modules |
| `GET /api/products` · `GET /api/products/{id}` | Warehouse | catalogue + stock (with currency) |
| `GET /api/customers` | Sales | the shared kernel customers |
| `GET /api/orders` · `GET /api/orders/{id}` · `POST /api/orders` | Sales | list / view / **place an order** |
| `GET /api/shipments` · `/{id}` · `POST /{id}/ship` · `/deliver` | Shipping | list / view / advance state |
| `GET /api/tickets` · `/{id}` · `POST /api/tickets` · `/{id}/messages` · `/{id}/status` | Support | tickets |

Every response uses the `ApiResponse<T>` envelope: `{ isSuccess, message, errors, data }`. All endpoints
except `register`/`login` require the auth cookie. Browse them at **`/swagger`**.

---

## Key packages (and why each earns its place)

| Package | Where | Why |
|---|---|---|
| **Microsoft.AspNetCore.Identity.EntityFrameworkCore** `10.0.9` | Kernel.Infrastructure | The EF Core **Identity stores** (UserManager/RoleManager over `KernelDbContext`) — a kernel concern, persisted in the one host context. |
| **Microsoft.Extensions.Identity.Stores** `10.0.9` | Kernel.Domain | The `IdentityUser<Guid>`/`IdentityRole<Guid>` base types that `ApplicationUser`/`ApplicationRole` extend. A deliberate, documented **Clean‑Architecture exception** (identity entities in the core) so no layer needs the kernel’s Infrastructure just to name the user. |
| **Microsoft.EntityFrameworkCore(.SqlServer)** `10.0.9` | Kernel.Infrastructure | The single host context, the generic `Repository<T>`, and `UnitOfWork`. The Application layer stays EF‑free — use cases depend on the repository abstractions instead. (The host also references `EntityFrameworkCore.Design` for migration tooling.) |
| **Ardalis.Result** `10.1.0` | Application, Kernel.Api | Result type every use case returns (`Success`/`NotFound`/`Invalid`), mapped to HTTP + `ApiResponse`. |
| **MediatR** `14.2.0` (+ **MediatR.Contracts** `2.0.1`) | Infrastructure / Application / Server / `Sales.Contracts` | The in‑process integration‑event bus. `OrderPlaced` is an `INotification`. Community licence is free for education; key optional via `MediatR:LicenseKey`. |
| **Swashbuckle.AspNetCore** `10.2.3` | Server | Swagger / OpenAPI UI at `/swagger`. |

`Ardalis.Specification` was **removed** and replaced by our **own** repositories — a generic
`IReadRepository<T>`/`IRepository<T>` + `IUnitOfWork` (modelled on the Platform / Social‑Media‑Platform
kernels), plus a specific repository where the generic one isn't enough — always returning entities. A
non‑entity read shape (Support's ticket‑list count projection) is a dedicated read‑only query object
(`ITicketSummaryQuery`), not a repository. The Application layer no longer references EF Core, so Clean
Architecture holds. `Ardalis.Result` stays.

NuGet versions are managed centrally in **`Directory.Packages.props`** (central package management), so all
projects reference each package by name and share one consistent set of versions.

---

## Prerequisites
- **.NET SDK 10** (`dotnet --version` → 10.x)
- **SQL Server** reachable as `localhost` with Windows auth (SQL Server 2022 Developer/Express is fine).
  Adjust the connection string in `src/ModularShop.Server/appsettings.json` if yours differs.
- **Node.js ≥ 20.19** (developed on **Node 24**) and **pnpm 11** (`npm i -g pnpm` or `corepack enable`).

No Docker required. The app **creates the `ModularShopDemo` database, applies the migration, and seeds
data automatically on first run**.

---

## How to run

### Backend (API + Swagger + auto‑migrate + seed)
```bash
dotnet run --project src/ModularShop.Server
```
Starts on **http://localhost:5080**. First run creates the database, applies the single migration, and
seeds currencies, customers, Identity users, the catalogue, orders, shipments and tickets. Explore at
**http://localhost:5080/swagger**.

**Sign in** with a seeded demo account (password **`Passw0rd!`**):

| Email | Role |
|---|---|
| `admin@modularshop.local` | Admin |
| `agent@modularshop.local` | Agent |

…or register a new account from the sign‑in screen.

### Frontend — development (hot reload)
```bash
pnpm --dir client install      # first time only
pnpm --dir client dev          # http://localhost:5173  (proxies /api → http://localhost:5080)
```
Run the backend too; open **http://localhost:5173** and sign in.

### Frontend — integrated (single deployable unit)
```bash
pnpm --dir client build        # emits the SPA into src/ModularShop.Server/wwwroot
dotnet run --project src/ModularShop.Server
```
Open **http://localhost:5080** — the host serves the React app *and* the API from one origin.

### Migrations (already generated; here for reference)
There is **one** centralised chain, owned by the host’s Infrastructure project and created with the
**official EF tool** (the migrations assembly is `ModularShop.Infrastructure`; the startup project is the host):
```bash
dotnet ef migrations add InitialCreate \
  --project src/ModularShop.Infrastructure \
  --startup-project src/ModularShop.Server \
  --context ModularShopDbContext
```
No design‑time factory is needed — `dotnet ef` boots the app's own service provider, so `AddModules`
selects the same modules the runtime uses and the one migration covers every enabled module's tables.
Migrations apply automatically at startup.

---

## Seed data
Generous and coherent, created on first run:
- **1 currency** (USD) and **10 customers** in the shared **kernel**. `Currency` stays a shared kernel
  entity referenced by both Warehouse and Sales — the demo just prices everything in USD for simplicity.
- Identity **roles** (Admin, Agent) and **2 demo users**.
- **18 products** across 6 categories (all priced in USD). Opening stock is already net of the historical
  orders below, so the catalogue is consistent with the order history.
- **7 historical orders** and **7 matching shipments** in varied states — including one **cancelled**
  order + shipment.
- **4 support tickets** with message threads (Open / Pending / Resolved / **Closed**) referencing the same customers.

---

## What was verified

Verified in this environment (WSL2 with the Windows .NET 10 toolchain + SQL Server 2022):

- ✅ `dotnet build` of the whole solution (**24 projects**) — 0 warnings, 0 errors.
- ✅ The reflection‑composed model matches the centralised migration: `dotnet ef migrations
  has-pending-model-changes` reports **no changes**. The running `ModularShopDemo` shows the five schemas
  `kernel` / `sales` / `warehouse` / `shipping` / `support`, child tables placed correctly, and
  cross‑schema FKs to the shared kernel `Customer` (Orders, Shipments, Tickets) and `Currency` (Orders,
  Products).
- ✅ Running the host created `ModularShopDemo`, applied the single migration, and seeded every module in
  order (kernel currencies/customers/Identity → Sales → Shipping → Support → Warehouse).
- ✅ **Module discovery + selection:** `GET /api` lists the loaded modules
  `["Kernel","Sales","Shipping","Support","Warehouse"]`. With `"Modules": ["Support"]`, only Kernel +
  Support compose — proven by `has-pending-model-changes` then reporting the model differs from the
  all‑module migration.
- ✅ **Auth:** unauthenticated `GET /api/products` → **401**; cookie login as `admin@…` → every module
  endpoint (`products`, `orders`, `shipments`, `tickets`) then returns **200**.
- ✅ **Live order→shipment flow:** `POST /api/orders` (2 × a product) → sync `IWarehouseApi` price/stock
  check, order saved (`PlacedBy` = the signed‑in user), `OrderPlaced` published; Warehouse **decremented
  that product's stock 110→108** and Shipping **created a `Pending` shipment (7→8)** — both on the one
  host context.
- ✅ Frontend (unchanged by this work): `pnpm typecheck` + `pnpm build` (pnpm 11 / Node 24) succeed; the
  host serves the SPA at `/` and its assets on the same origin.

---

## Notes & non‑goals
- **No CQRS command/query bus, no test projects, no Docker** (per the brief). MediatR is used **only**
  for integration events. Boundaries are enforced by project references + the `*.Contracts` surface;
  a **NetArchTest** "tripwire" project is the documented next step.
- The in‑process MediatR bus loses events if the process crashes mid‑handling; the production upgrade is
  a transactional **outbox** behind the same publish call.
- The solution uses the modern **`.slnx`** format — build with the CLI or a recent Visual Studio (17.13+) / Rider.

Deep dives: [`docs/architecture.md`](docs/architecture.md) ·
[`docs/decision-log.md`](docs/decision-log.md) · [`docs/platform-mapping.md`](docs/platform-mapping.md).
