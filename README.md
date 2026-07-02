# ModularShop — a Modular Monolith you can read in an afternoon

A small, **correct**, runnable **Modular Monolith (MM)** in **ASP.NET Core (.NET 10) + React**, on
**MSSQL**. It exists to teach the MM architecture by example and to serve as the *target design* for
migrating the existing `../Platform` solution.

Three business modules — **Sales**, **Warehouse**, **Shipping** — run **in one process, deploy as one
unit, and share one database with a schema each**. One realistic flow (place an order → check
price/stock → decrement stock → create a shipment) demonstrates **both** ways modules talk to each
other.

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

```
ModularShop/
├─ ModularShop.slnx                      # solution (modern .slnx format; use CLI or a recent VS/Rider)
├─ README.md
├─ docs/
│  ├─ architecture.md                    # the MM concepts + Mermaid diagrams
│  ├─ decision-log.md                    # every architectural decision + alternatives
│  └─ platform-mapping.md                # what we took from ../Platform + a transition plan
├─ src/
│  ├─ ModularShop.Api/                   # HOST = composition root (Program.cs). No business logic.
│  │                                     #   serves the built React SPA from wwwroot (one unit)
│  ├─ ModularShop.SharedKernel/          # kernel: Entity, Result, IEventBus, IRepository, ICurrentUser
│  ├─ ModularShop.SharedKernel.Infrastructure/   # ModuleDbContext, EfRepository, InMemoryEventBus
│  ├─ ModularShop.SharedKernel.Web/      # IModule, ApiResponse, exception middleware, CurrentUser
│  └─ Modules/
│     ├─ Sales/       ModularShop.Modules.Sales           # customers, orders, PlaceOrder flow
│     │               ModularShop.Modules.Sales.Contracts # PUBLIC: OrderPlaced event
│     ├─ Warehouse/   ModularShop.Modules.Warehouse           # products, stock, event handler
│     │               ModularShop.Modules.Warehouse.Contracts # PUBLIC: IWarehouseApi, ProductStock
│     └─ Shipping/    ModularShop.Modules.Shipping          # shipments (no .Contracts — nothing calls it)
└─ client/                               # React + TypeScript SPA (Vite, pnpm); builds into Api/wwwroot
```

| Project | Role |
|---|---|
| `ModularShop.Api` | Composition root: wires modules via `IModule`, runs migrations+seed, serves the SPA. No business logic. |
| `ModularShop.SharedKernel` | Pure domain primitives (no framework deps). |
| `ModularShop.SharedKernel.Infrastructure` | EF base context, repository, in‑process event bus. |
| `ModularShop.SharedKernel.Web` | ASP.NET cross‑cutting: `IModule`, `ApiResponse`, middleware, current‑user. |
| `Modules.Sales` (+ `.Contracts`) | Orders & customers; publishes `OrderPlaced`; calls `IWarehouseApi`. |
| `Modules.Warehouse` (+ `.Contracts`) | Catalogue & stock; implements `IWarehouseApi`; handles `OrderPlaced`. |
| `Modules.Shipping` | Shipments; handles `OrderPlaced`; exposes no public contract. |

Boundary rule (compile‑time enforced): a module may reference **only** the shared kernel and other
modules’ `*.Contracts` — never another module’s implementation.

---

## The demo domain & flow

Placing an order exercises the two inter‑module communication styles in one request:

1. **Synchronous, via a public interface** — Sales calls `IWarehouseApi.GetProductsAsync(...)` to get
   the current **price + stock** (it needs the answer *now*). Sales never sees Warehouse’s tables.
2. **Asynchronous, via an integration event** — Sales saves the order, then publishes `OrderPlaced` on
   the in‑process event bus. **Warehouse** decrements stock and **Shipping** creates a `Pending`
   shipment — each in its own schema, each reacting independently.

See the sequence diagram in [`docs/architecture.md`](docs/architecture.md#the-order--shipment-flow).

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

---

## Prerequisites
- **.NET SDK 10** (`dotnet --version` → 10.x)
- **SQL Server** reachable as `localhost` with Windows auth (SQL Server 2022 Developer/Express is fine).
  Adjust the connection string in `src/ModularShop.Api/appsettings.json` if yours differs — e.g. for SQL
  auth use `Server=localhost;Database=ModularShopDemo;User Id=sa;Password=***;TrustServerCertificate=True`.
- **Node.js 20+** and **pnpm 9** (`npm i -g pnpm@9`) for the frontend.

No Docker required. The app **creates the `ModularShopDemo` database, applies migrations, and seeds
data automatically on first run**.

---

## How to run

### Backend (API + auto‑migrate + seed)
```bash
dotnet run --project src/ModularShop.Api
```
Starts on **http://localhost:5080**. On first run it creates the database, applies all migrations, and
seeds the catalogue, customers, orders and shipments. Try http://localhost:5080/api/products.

### Frontend — development (hot reload)
```bash
pnpm --dir client install     # first time only
pnpm --dir client dev         # http://localhost:5173  (proxies /api → http://localhost:5080)
```
Run the backend too; open **http://localhost:5173**.

### Frontend — integrated (single deployable unit)
```bash
pnpm --dir client build       # emits the SPA into src/ModularShop.Api/wwwroot
dotnet run --project src/ModularShop.Api
```
Open **http://localhost:5080** — the API now serves the React app *and* the API from one origin: the
Modular Monolith’s "one deployable unit" made concrete.

### Migrations (already generated; here for reference)
Each module owns its migrations, created with the **official EF tool**:
```bash
dotnet ef migrations add InitialCreate \
  --project src/Modules/Warehouse/ModularShop.Modules.Warehouse \
  --startup-project src/ModularShop.Api --context WarehouseDbContext
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

- ✅ `dotnet build` of the whole solution — **0 warnings, 0 errors**.
- ✅ `dotnet ef migrations add` for all three contexts — via the official tool.
- ✅ Running the API created `ModularShopDemo` and produced **schema‑per‑module** with a per‑schema
  migrations‑history table (nothing in `dbo`); seed counts confirmed via `sqlcmd`
  (18 products, 10 customers, 6 orders / 10 lines, 6 shipments / 10 items).
- ✅ **Live flow:** `POST /api/orders` → synchronous `IWarehouseApi` price/stock check, order saved,
  `OrderPlaced` published; Warehouse **decremented stock (140→137, 95→93)** and Shipping **created a
  shipment (6→7)** — both event handlers fired (confirmed in logs). `POST .../ship` advanced the
  shipment to `Shipped` with carrier + tracking.
- ✅ Frontend: `pnpm install` + `pnpm build` succeeded; the API serves the SPA at `/`, its assets, and
  the SPA fallback for client routes, with the API on the same origin.

### You may need to do yourself
- Point the connection string at **your** SQL Server if it isn’t `localhost` / Windows auth.
- If a login like `gcloud`/`az`/SQL setup is needed interactively, run it in your own shell.

---

## Notes & non‑goals
- **No CQRS command/query bus, no test projects, no Docker** (per the brief). Boundaries are enforced
  by project references + `internal`; adding a NetArchTest project is the documented next step.
- **No MediatR** — the ~30‑line `InMemoryEventBus` makes integration events transparent (and MediatR is
  commercially licensed since 2025). Swapping in MediatR `INotification` later is trivial.
- The solution uses the modern **`.slnx`** format — build with the CLI or open in a recent
  Visual Studio (17.13+) / Rider.

Deep dives: [`docs/architecture.md`](docs/architecture.md) ·
[`docs/decision-log.md`](docs/decision-log.md) · [`docs/platform-mapping.md`](docs/platform-mapping.md).
