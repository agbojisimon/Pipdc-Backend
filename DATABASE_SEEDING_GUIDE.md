# Database Seeding Strategies — When to Seed, When to Import, When to Let Users Create

> Research conducted August 2026. Sources: Microsoft EF Core docs, industry articles, production system patterns.

---

## The Two Types of Seed Data

This is the most important distinction. Most teams conflate them.

| | Reference Data | Operational/Test Data |
|---|---|---|
| **What** | Countries, states, currencies, roles, status codes | Fake users, sample orders, demo properties |
| **Lives in** | Production + all environments | Dev/test/staging only |
| **Changes** | Rarely (a new state, a renamed role) | Constantly |
| **Source of truth** | The application — these rows define the app's contract | Generated or fabricated |
| **Should ship with the app** | Yes | Never |

**Rule of thumb:** If the app crashes without this data, it's reference data. If the app just looks empty, it's test data.

---

## The Three Approaches

### Approach 1: EF Core `HasData` (Model-Managed Data)

```csharp
builder.Entity<State>().HasData(
    new State { Id = 1, Name = "Lagos", Slug = "lagos" },
    new State { Id = 2, Name = "Plateau", Slug = "plateau" }
);
```

**Microsoft's current classification:** "Model managed data" — NOT general-purpose seeding.

| Pros | Cons |
|---|---|
| Version-controlled in code | Data is captured in migration snapshots (files grow huge with large datasets) |
| Runs during `dotnet ef database update` | Must specify explicit PKs even if DB generates them |
| Idempotent | Changing a PK causes EF to DELETE and re-insert the row |
| Portable across DB providers | Only runs during migrations/EnsureCreated — not at app startup |
| Works with `dotnet ef migrations script` | Not suitable for data that depends on DB state or needs external API calls |

**Best for:** Small, static reference data (< 100 rows) that rarely changes. Example: 37 Nigerian states, 5 blog categories, 3 user roles.

**Limitations (from Microsoft docs):**
- Previously inserted data will be removed if the PK is changed
- Not suitable for data that isn't fixed and deterministic (e.g., `DateTime.Now`)
- Not suitable for large datasets (migration snapshots become huge)
- Not suitable for data that needs calls to external APIs

---

### Approach 2: `UseSeeding` / `UseAsyncSeeding` (EF Core 7+)

```csharp
// In Program.cs or DbContext
optionsBuilder.UseSeeding((context, _) =>
{
    var states = context.Set<State>().ToList();
    if (!states.Any())
    {
        context.Set<State>().AddRange(
            new State { Name = "Lagos", Slug = "lagos" },
            new State { Name = "Plateau", Slug = "plateau" }
        );
        context.SaveChanges();
    }
});
```

**Microsoft's recommended approach** for general-purpose seeding.

| Pros | Cons |
|---|---|
| Runs AFTER migrations are applied (separate from schema) | Slightly more code than HasData |
| Not captured in migration snapshots | Synchronous delegate required (EF tools limitation) |
| Can check existing data before inserting (idempotent via logic, not PKs) | Runs only on `Migrate()` / `EnsureCreated()` / migration bundles |
| Supports `SaveChangesAsync` with business rules | Not called by external SQL scripts |
| Can use external API calls, password hashing, etc. | |

**Best for:** Data that needs conditional logic, depends on other data, or is too large for `HasData`. Example: seeding admin user with hashed password, default settings, feature flags.

---

### Approach 3: External Dataset Import (CSV/JSON/Package)

```bash
# One-time import from a geo dataset
psql -d pipdc -c "\COPY locations(name, slug, type, parent_id) FROM 'nigerian_states.csv' CSV HEADER"
```

Or via a dedicated package:
```bash
dotnet add package GeoNames  # hypothetical
# or
import pycountry  # Python equivalent
```

| Pros | Cons |
|---|---|
| Scales to thousands/millions of rows | External dependency or file to maintain |
| Data is authoritative (sourced from official registries) | Not version-controlled in code (unless you commit the CSV) |
| No migration snapshot bloat | Requires a separate import step |
| Can be updated independently of code releases | |
| Works for hierarchical data (countries → states → cities → zip codes) | |

**Best for:** Large geographic datasets (countries, states, LGAs, cities, zip codes), currency lists, ISO codes. Anything with 100+ rows that comes from an authoritative external source.

**Industry examples:**
- Laravel: `laravel-countries` package (ships with countries, states, translations in 9 languages)
- MySQL: REST Countries dataset (countries + states + cities in CSV/JSON/SQL)
- Postgres: GeoNames dataset (120,000+ places)
- Atlas: Declares seed data alongside schema in HCL/SQL blocks

---

## Decision Matrix

| Question | If Yes → | If No → |
|---|---|---|
| Is it < 100 rows? | `HasData` or `UseSeeding` | External dataset import |
| Does it rarely change? | `HasData` | `UseSeeding` or import |
| Is it part of the app's contract (app breaks without it)? | `HasData` | `UseSeeding` or admin-created |
| Does it come from an authoritative external source? | Import from dataset | Hand-write it |
| Does it need conditional logic or DB queries? | `UseSeeding` | `HasData` |
| Will it grow over time (users add to it)? | Admin API + empty seed | `HasData` |
| Does it depend on other data (FKs)? | `UseSeeding` (can query first) | `HasData` (if IDs are deterministic) |

---

## Nigerian Locations — Recommended Approach

| Tier | Rows | Approach | Why |
|---|---|---|---|
| **States** | 37 | `HasData` in migration | Small, static, app needs them at startup |
| **LGAs** | 774 | Admin-created via API OR CSV import | Too large for HasData; not all are needed at launch |
| **Cities** | 1000+ | Admin-created via API | Dynamic, grows as properties are listed |
| **Areas/Neighbourhoods** | varies | Admin-created via API | Hyper-local, changes over time |

**If you need all 774 LGAs at launch:** Import from a CSV/JSON file via a one-time migration script or `UseSeeding` — do NOT hand-type them in `HasData`.

---

## Key Takeaways

1. **`HasData` is for small, static reference data** — not for large datasets. Microsoft now classifies it as "model managed data" and recommends `UseSeeding` for general-purpose use.

2. **Seeding ≠ Importing.** Seeding is for data the app defines. Importing is for data the world defines (countries, states, currencies).

3. **Reference data ships with the app. Test data never reaches production.** Keep them in separate steps.

4. **For hierarchical geographic data,** the industry standard is external datasets (GeoNames, REST Countries) or dedicated packages — not hand-written seed code.

5. **`UseSeeding` is the modern EF Core approach** — it runs after migrations, supports conditional logic, and doesn't bloat migration snapshots.

6. **Idempotency matters.** A seed that breaks on its second run breaks CI. Use `ON CONFLICT DO NOTHING`, check-before-insert, or upsert patterns.

---

## References

- [EF Core Data Seeding (Microsoft Docs)](https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding)
- [Database Seeding: When to Hit the Table vs. When to Hit the API](https://www.sqlserverscience.com/tools/database-seeding-when-to-hit-the-table-vs-when-to-hit-the-api/)
- [Stop Calling External APIs for Country Data](https://dev.to/lwwcas/stop-calling-external-apis-for-country-data-in-laravel-seed-it-into-your-database-instead-k7a)
- [Stop Hardcoding Country Lists — Use a Proper World Geo Dataset](https://dev.to/rakshitshah94/stop-hardcoding-country-lists-use-a-proper-world-geo-dataset-mysql-json-csv-5gaf)
- [Seed Data as Code: Lookup Tables and Reference Data (Atlas)](https://atlasgo.io/guides/seed-data-as-code)
- [Database Seeding in 2026: 7 Methods That Work](https://seedfa.st/blog/database-seeding)
