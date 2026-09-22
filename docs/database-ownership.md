# Database ownership

| Service | Database / owner | Current schema |
|---|---|---|
| Catalog | catalog_db / catalog_app | categories, products, __EFMigrationsHistory |
| Identity | identity_db / identity_app | No application tables yet |
| Inventory | inventory_db / inventory_app | No application tables yet |
| Ordering | ordering_db / ordering_app | No application tables yet |

Each service migrates only its own database with its own restricted login. Cross-service connections
are denied; the bootstrap administrator is not an application credential. No service references another
service's projects or joins its tables. Later HTTP/events convey IDs and snapshots across boundaries.

Catalog's products.category_id foreign key targets categories.id in the same database with RESTRICT delete.
A unique index protects categories.normalized_name against concurrent duplicates. Product prices use
numeric(10,2) with a nonnegative/range check; Domain rejects excess precision before SQL could round it.
Catalog owns authoritative price; future Ordering stores price/name snapshots rather than foreign keys
to this database. Catalog has no stock fields or inventory joins.

The migration is `20260922174057_InitialCatalog`. Normal development tables use public; integration tests
use a random owned schema with a separate migration history and clean it afterward. No volume resets
are part of Phase 3. [Catalog commands and seed IDs](catalog.md) and [infrastructure grants](docker.md).
