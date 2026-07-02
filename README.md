# ModularShop — a Modular Monolith you can read in an afternoon

A small, **correct**, runnable **Modular Monolith (MM)** in **ASP.NET Core (.NET 10) + React**, on
**MSSQL**. It exists to teach the MM architecture by example and to serve as the *target design* for
migrating the existing `../Platform` solution.

Three business modules — **Sales**, **Warehouse**, **Shipping** — run **in one process, deploy as one
unit, and share one database with a schema each**. One realistic flow (place an order → check
price/stock → decrement stock → create a shipment) demonstrates **both** ways modules talk to each
other. Every module is a small **Clean Architecture** stack of its own (Domain · Application ·
Infrastructure · Api), so the example also shows how MM and Clean Architecture fit together.

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
- You want clean, enforceable boundaries **without** the operational cost of microservices (no network
  hops, distributed transactions, or 10 pipelines).
- Early‑stage / evolving products where the right service boundaries aren’t clear yet — *start
  modular, extract services later only if you must.*
- You’re untangling a "big ball of mud" and want a safe, incremental path to strong modularity.
- A single team/codebase, one database, and one deploy is fine — you don’t need independent scaling or
  a polyglot stack (yet).

### When **not** to
- You genuinely need independent deployment/scaling per capability, or teams that must release fully
  independently → consider microservices (and pay their premium deliberately).

---

## Solution layout

Every **module** is split into one project **per Clean‑Architecture layer** (Domain, Application,
Infrastructure, Api), plus a tiny public `*.Contracts` project where other modules are allowed to look.
The **Kernel** (shared cross‑cutting code) is layered the same way.

```
ModularShop/
├─ ModularShop.slnx                      # solution (modern .slnx format; use CLI or a recent VS/Rider)
├─ README.md
├─ docs/
│  ├─ architecture.md                    # the MM concepts + Mermaid diagrams
│  ├─ decision-log.md                    # every architectural decision + alternatives
│  └─ platform-mapping.md                # what we took from ../Platform + a transition plan
├─ src/
│  ├─ ModularShop.Server/                # HOST = composition root (Program.cs). No business logic.
│  │                                     #   registers modules, MediatR, controllers (ApplicationParts),
│  │                                     #   Swagger; serves the built React SPA from wwwroot (one unit)
│  ├─ Kernel/                            # the shared kernel — itself Clean Architecture, kept lean
│  │  ├─ ModularShop.Kernel.Domain            # Entity
│  │  ├─ ModularShop.Kernel.Application       # ICurrentUser (cross-cutting abstractions)
│  │  ├─ ModularShop.Kernel.Infrastructure    # ModuleDbContext, EfRepository (Ardalis), IModule, options
│  │  └─ ModularShop.Kernel.Web               # ApiResponse, ApiControllerBase, exception middleware, CurrentUser
│  └─ Modules/
│     ├─ Sales/        .Domain · .Application · .Infrastructure · .Api  (+ .Contracts: OrderPlaced)
│     ├─ Warehouse/    .Domain · .Application · .Infrastructure · .Api  (+ .Contracts: IWarehouseApi)
│     └─ Shipping/     .Domain · .Application · .Infrastructure · .Api  (no .Contracts — nothing calls it)
└─ client/                               # React + TypeScript SPA (Vite, pnpm); builds into Server/wwwroot
```

Inside each `*.Infrastructure` project, the DbContext, EF configurations and **migrations** live under
a `Persistence/` folder (`Persistence/Migrations/`).

| Layer (per module) | Contents | May reference |
|---|---|---|
| `*.Domain` | Entities, enums, domain logic | Kernel.Domain |
| `*.Application` | Use cases, DTOs, Ardalis Specifications, mappings | Domain, Kernel.Application, other modules’ `*.Contracts`, own `*.Contracts` |
| `*.Infrastructure` | DbContext + `Persistence/` (configs, migrations), `IModule`, seeding, integration‑event handlers | Application, Domain, Kernel.Infrastructure, other modules’ `*.Contracts` |
| `*.Api` | Controllers only (invoke use cases, return `ApiResponse`) | Application, Kernel.Web |
| `*.Contracts` | The public surface other modules may use (interfaces, DTOs, integration events) | nothing (or `MediatR.Contracts` for events) |

**Boundary rule (compile‑time):** a module’s projects reference **only** the Kernel and other modules’
`*.Contracts` — never another module’s Domain/Application/Infrastructure/Api. The host is the only
project that references every module.

---

## The demo domain & flow

Placing an order exercises the two inter‑module communication styles in one request:

1. **Synchronous, via a public interface** — Sales calls `IWarehouseApi.GetProductsAsync(...)` to get
   the current **price + stock** (it needs the answer *now*). Sales never sees Warehouse’s tables.
2. **Asynchronous, via an integration event** — Sales saves the order, then publishes `OrderPlaced`
   (a MediatR `INotification`). **Warehouse** decrements stock and **Shipping** creates a `Pending`
   shipment — each reacting independently in its own schema.

See the sequence diagram in [`docs/architecture.md`](docs/architecture.md#the-order--shipment-flow).

Requests flow **Controller → Use case → (Repository via Ardalis Specification | IWarehouseApi | MediatR)**.
Controllers hold no logic; they invoke a single use case and wrap its `Ardalis.Result` in an `ApiResponse<T>`.

### API endpoints
| Method & path | Module | Purpose |
|---|---|---|
| `GET /api` | host | app info + loaded modules |
| `GET /api/products` · `GET /api/products/{id}` | Warehouse | catalogue + stock |
| `GET /api/customers` | Sales | customers |
| `GET /api/orders` · `GET /api/orders/{id}` | Sales | list / view orders |
| `POST /api/orders` | Sales | **place an order** (the flow) |
| `GET /api/shipments` · `GET /api/shipments/{id}` | Shipping | list / view shipments |
| `POST /api/shipments/{id}/ship` · `.../deliver` | Shipping | advance shipment state |

Every response uses the `ApiResponse<T>` envelope: `{ isSuccess, message, errors, data }`.
Browse and try them all at **`/swagger`**.

---

## Key packages (and why each earns its place)

| Package | Where | Why |
|---|---|---|
| **Ardalis.Result** `10.1.0` | Application, Kernel.Web | Result type used by every use case (`Success` / `NotFound` / `Invalid`), mapped to HTTP status + `ApiResponse` in the base controller. Replaces the hand‑rolled `Result`. |
| **Ardalis.Specification** `9.3.1` (+ `.EntityFrameworkCore`) | Application / Kernel.Infrastructure | Queries are `Specification` objects in the Application layer; the generic repository runs them. This lets the Application layer query **without** referencing EF or the DbContext — the clean way to keep the dependency rule. Replaces the hand‑rolled repository. |
| **MediatR** `14.1.0` | Infrastructure (handlers), Application (publish), Server (registration) | The in‑process integration‑event bus. `OrderPlaced` is an `INotification`; Warehouse and Shipping handle it with `INotificationHandler<OrderPlaced>`. MediatR’s **Community** licence is free for education/small orgs; a key is optional (see `MediatR:LicenseKey` in `appsettings.json`). Replaces the hand‑rolled event bus. |
| **MediatR.Contracts** `2.0.1` | `Sales.Contracts` | Lets the contracts project mark `OrderPlaced` as an `INotification` without taking the full MediatR runtime. |
| **Swashbuckle.AspNetCore** `10.2.3` | Server | Swagger / OpenAPI UI at `/swagger`. |

---

## Prerequisites
- **.NET SDK 10** (`dotnet --version` → 10.x)
- **SQL Server** reachable as `localhost` with Windows auth (SQL Server 2022 Developer/Express is fine).
  Adjust the connection string in `src/ModularShop.Server/appsettings.json` if yours differs — e.g. for
  SQL auth use `Server=localhost;Database=ModularShopDemo;User Id=sa;Password=***;TrustServerCertificate=True`.
- **Node.js ≥ 20.19** (developed on **Node 24**) and **pnpm 11** (`npm i -g pnpm` or `corepack enable`).

No Docker required. The app **creates the `ModularShopDemo` database, applies migrations, and seeds
data automatically on first run**.

---

## How to run

### Backend (API + Swagger + auto‑migrate + seed)
```bash
dotnet run --project src/ModularShop.Server
```
Starts on **http://localhost:5080**. On first run it creates the database, applies all migrations, and
seeds the catalogue, customers, orders and shipments. Open **http://localhost:5080/swagger** to explore
the API, or hit http://localhost:5080/api/products directly.

### Frontend — development (hot reload)
```bash
pnpm --dir client install      # first time only
pnpm --dir client dev          # http://localhost:5173  (proxies /api → http://localhost:5080)
```
Run the backend too; open **http://localhost:5173**.

### Frontend — integrated (single deployable unit)
```bash
pnpm --dir client build        # emits the SPA into src/ModularShop.Server/wwwroot
dotnet run --project src/ModularShop.Server
```
Open **http://localhost:5080** — the host now serves the React app *and* the API from one origin: the
Modular Monolith’s "one deployable unit" made concrete.

### Migrations (already generated; here for reference)
Each module owns its migrations under `Infrastructure/Persistence/Migrations`, created with the
**official EF tool**:
```bash
dotnet ef migrations add InitialCreate \
  --project src/Modules/Warehouse/ModularShop.Modules.Warehouse.Infrastructure \
  --startup-project src/ModularShop.Server \
  --context WarehouseDbContext -o Persistence/Migrations
# …and likewise for SalesDbContext and ShippingDbContext.
```
Migrations are applied automatically at startup, so you normally don’t run `database update` yourself.

---

## Seed data
Generous and coherent, created on first run:
- **18 products** across 6 categories (Peripherals, Displays, Audio, Storage, Networking, Accessories).
- **10 customers**.
- **6 historical orders** (10 lines) in varied states, and **6 matching shipments** (10 items) in
  `Pending` / `Shipped` / `Delivered` states — so every screen is populated immediately.

---

## What was verified

On the build machine (WSL2 with the Windows .NET 10 toolchain + SQL Server 2022):

- ✅ `dotnet build` of the whole solution (19 projects) — **0 warnings, 0 errors**.
- ✅ `dotnet ef migrations add` for all three contexts — via the official tool, output under each
  module’s `Infrastructure/Persistence/Migrations`.
- ✅ Running the host created `ModularShopDemo` with **schema‑per‑module** and a per‑schema
  migrations‑history table (nothing in `dbo`); seed counts confirmed via `sqlcmd`
  (18 products, 10 customers, 6 orders / 10 lines, 6 shipments / 10 items).
- ✅ **Live flow:** `POST /api/orders` → synchronous `IWarehouseApi` price/stock check, order saved,
  `OrderPlaced` published on MediatR; **both** `INotificationHandler<OrderPlaced>` fired — Warehouse
  **decremented stock (140→137, 95→93)** and Shipping **created a shipment (6→7)** (confirmed in logs).
  `POST .../ship` advanced the shipment to `Shipped` with carrier + tracking. Validation → `400`,
  not‑found → `404`, both as an `ApiResponse`.
- ✅ **Swagger** at `/swagger` lists all 10 controller endpoints (controllers discovered from every
  module’s Api assembly via MVC ApplicationParts).
- ✅ Frontend: `pnpm install` + `pnpm build` (pnpm 11 / Node 24) succeeded; the host serves the SPA at
  `/`, its assets, and the SPA fallback for client routes, on the same origin.

### You may need to do yourself
- Point the connection string at **your** SQL Server if it isn’t `localhost` / Windows auth.
- If a login/setup step needs an interactive shell, run it yourself.

---

## Notes & non‑goals
- **No CQRS command/query bus, no test projects, no Docker** (per the brief). MediatR is used **only**
  for integration events (`INotification`), not as a command/query bus. Boundaries are enforced by
  project references + the `*.Contracts` surface; adding a **NetArchTest** project is the documented
  next step.
- The in‑process MediatR bus loses events if the process crashes mid‑handling; the production upgrade
  is a transactional **outbox** behind the same publish call — see `docs/architecture.md`.
- The solution uses the modern **`.slnx`** format — build with the CLI or open in a recent
  Visual Studio (17.13+) / Rider.

Deep dives: [`docs/architecture.md`](docs/architecture.md) ·
[`docs/decision-log.md`](docs/decision-log.md) · [`docs/platform-mapping.md`](docs/platform-mapping.md).
