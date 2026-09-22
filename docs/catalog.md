# Catalog guide

Phase 3 implements the first business service. Catalog owns products, categories and prices.
It does not own stock, orders or authentication. The API is a localhost learning service.

## Start and verify

From the repository root in PowerShell 7.3+, with the ignored `.env` already created:

```powershell
docker compose up -d --wait --wait-timeout 120
dotnet restore MicroShop.sln
dotnet tool restore
./scripts/Set-ServiceEnvironment.ps1 -Service Catalog
dotnet ef database update --project src/Services/Catalog/Catalog.Infrastructure
dotnet build MicroShop.sln --no-restore
dotnet run --project src/Services/Catalog/Catalog.Api --no-build --launch-profile http -- --seed
dotnet run --project src/Services/Catalog/Catalog.Api --no-build --launch-profile http
```

Open http://localhost:5220/swagger (Development only); the document is `/openapi/v1.json`.
The connection helper sets `ConnectionStrings__Database` only in the current terminal, without printing it.
Catalog validates the database/login as `catalog_db`/`catalog_app`. Never use the bootstrap administrator.
Migrations and seeding are explicit commands; starting the HTTP host does neither.
Stop the host with Ctrl+C. Run tests in another terminal:

```powershell
./scripts/Test-Catalog.ps1
./scripts/Test-Skeleton.ps1
```

The first runs all 30 tests (9 unit/application, 17 PostgreSQL integration, 4 architecture).
It prepares the test connection privately and restores prior environment values afterward.
Integration tests create/migrate/seed a random `catalog_test_<guid>` schema under catalog_app,
exclude public from SearchPath and drop only that generated schema on teardown. They require a
reachable local PostgreSQL; missing configuration fails clearly rather than silently skipping.
An interrupted test process can leave its disposable schema behind; inspect ownership before removing it.
`Test-Skeleton.ps1` checks seven HTTP hosts, the Catalog database read and Swagger delivery, then stops hosts.
It requires a built solution and applied Catalog migration; it does not drive a browser.

## HTTP contract

All routes start with `/api/catalog`. IDs are UUIDs. JSON uses camelCase.

| Method | Route | Success |
|---|---|---|
| GET | `/categories`, `/products` | 200 paginated result |
| GET | `/categories/{id}`, `/products/{id}` | 200 DTO |
| POST | `/categories`, `/products` | 201 DTO and Location header |
| PUT | `/categories/{id}`, `/products/{id}` | 200 updated DTO; full replacement |
| DELETE | `/categories/{id}`, `/products/{id}` | 204 |

Category body: `{"name":"Accessories","description":"Optional description"}`.
Product body: `{"name":"Mouse","description":"Wireless","unitPrice":29.99,"categoryId":"11111111-1111-1111-1111-111111111112"}`.
Product responses add `id`, `categoryName` and `currency: "USD"`. This learning catalog uses one currency;
there is no conversion, taxation or inventory quantity on a product.

List parameters: `page` (default 1, minimum 1), `pageSize` (default 20, 1–100),
`search` (optional, trimmed, maximum 120 characters); products also accept `categoryId`.
Results contain `items`, `totalCount`, `page`, `pageSize`, ordered by name then ID.
Search is case-insensitive literal substring matching on name/description; `%`, `_` and backslash
are escaped and SQL values are parameterized. Beyond-last-page results are empty with the matching total.
A nonexistent category filter returns an empty result. Count and page are separate statements and may
differ briefly during concurrent edits; this phase does not promise a transactionally frozen browsing view.

Names are required/trimmed: category maximum 80 characters, product maximum 120.
Descriptions are optional (stored as empty strings), maximum 500/2000 respectively.
Price and category ID are required; price is 0–99,999,999.99 with at most two decimal places.
Category names are unique after trimming and invariant uppercase normalization.
Products must refer to an existing category; delete/move products before deleting their category.
PUT uses last-write-wins; there are no ETags or optimistic version tokens yet.

Errors use `application/problem+json`, a status/title/traceId and field `errors` for business validation:
400 invalid body/query/domain values, 404 absent resource/route, 409 duplicate category or FK conflict.
Malformed JSON also returns 400. Unexpected exceptions return a generic 500 with details in server logs.
This phase does not add authentication; Swagger and seed execution are restricted to Development.

## Seed and database workflow

The explicit `--seed` command inserts missing deterministic IDs in one transaction, never updates existing rows.
Run it sequentially; concurrent seed jobs are not supported. A conflicting category name with a different ID
fails atomically: rename/remove that development conflict deliberately, then rerun; the seed does not reset data.

| Product | ID | Price (USD) | Category |
|---|---|---|---|
| Laptop | `22222222-2222-2222-2222-222222222221` | 999.99 | Computers |
| Keyboard | `22222222-2222-2222-2222-222222222222` | 79.99 | Accessories |
| Mouse | `22222222-2222-2222-2222-222222222223` | 29.99 | Accessories |
| Monitor | `22222222-2222-2222-2222-222222222224` | 249.99 | Computers |
| Headphones | `22222222-2222-2222-2222-222222222225` | 59.99 | Accessories |

Category IDs: Computers `11111111-1111-1111-1111-111111111111`,
Accessories `11111111-1111-1111-1111-111111111112`.
Future Inventory seeds can repeat product IDs in their own data without querying Catalog's database.

Migration `20260922174057_InitialCatalog` creates `categories`, `products`, their indexes/constraints and
EF's history table only. To review SQL or check model drift after setting the service environment:

```powershell
dotnet ef migrations script --project src/Services/Catalog/Catalog.Infrastructure --output artifacts/catalog-migration.sql
dotnet ef migrations has-pending-model-changes --project src/Services/Catalog/Catalog.Infrastructure
```

The first empty-database migration can log a failed history-table SELECT before creating the table;
the command must still finish successfully. Do not reset volumes to resolve ordinary migration issues.
See [EF migration guidance](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)
and [ASP.NET integration testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0).
