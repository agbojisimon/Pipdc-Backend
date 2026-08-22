# PIPDC — Coding Patterns & Best Practices

## Patterns Used

### 1. Layered Architecture (Clean Architecture)
- Domain → Entities, Enums, Result<T>, Error
- Application → Interfaces, DTOs, Service implementations
- Infrastructure → DbContext, Identity, Email, Auth
- API → Controllers (thin HTTP layer)

### 2. DTO Pattern
- Response DTOs → `record` (immutable, value equality)
- Query parameters → `class` with setter guards (mutable, validated on set)

### 3. Service-to-Controller Mapping
- Every service method maps to exactly one controller endpoint
- No service method called from multiple controllers (except intentional shared services)

### 4. Result Pattern
- Every service method returns `Result<T>` or `Result`
- No exceptions for business logic errors
- Error codes: `{entity}.{rule}` — lowercase, dot-separated

### 5. Controller Thinness
- Controllers do exactly 3 things: extract JWT claims, call service, return `result.ToActionResult()`
- No business logic, no data access, no error handling in controllers

### 6. Authorization
- Class-level `[Authorize(Roles)]` for entirely admin-only controllers
- Method-level for mixed auth controllers
- No attribute for public/anonymous

### 7. Identity for User Management
- Soft-deactivation via `LockoutEnd = DateTimeOffset.MaxValue`
- Reactivation via `LockoutEnd = null`
- No custom `IsActive` field — use Identity's built-in mechanism

### 8. DI Registration
- Register where implemented, not where interface is defined
- Application services in `Application.DependencyInjection.cs`
- Infrastructure services registered in Infrastructure layer

### 9. File Organization
- One folder per domain area
- Interface + implementation in same folder
- DTOs in single `Dtos.cs` per folder

### 10. Data Access
- Services use `IAppDbContext` (Application layer), never `AppDbContext` directly
- Exception: `UserManager<T>` injected directly from Identity

### 11. Toggle Pattern (Step 4)
For simple boolean flips (verify, feature, activate), use a dedicated toggle endpoint:
- No request body needed — the ID comes from the route
- Service loads entity → flips boolean → sets UpdatedAt → saves → returns updated DTO
- Client doesn't need to know current state — just call to toggle
- Use `PUT` (idempotent), not `POST`

### 12. Summary/Stats Pattern (Step 5)
For endpoints that return an entity plus related counts:
- Create a dedicated summary DTO (e.g., `AgentSummaryDto`) — don't overload the base DTO
- Keep the base DTO lightweight for list views
- Count related entities in separate queries ( EF Core translates to SQL)
- Summary endpoint is typically public/read-only, no auth needed

### 13. Relationship Counting (Step 5)
Three patterns for counting related entities:
- **Direct FK:** `db.Conversations.Where(c => c.AgentId == id).CountAsync()`
- **Through navigation:** `db.Enquiries.Where(e => e.Property.AgentId == id).CountAsync()`
- **Owned collection:** `db.Properties.Where(p => p.AgentId == id).CountAsync()`
EF Core handles the join logic — no manual SQL needed.

### 14. No Request Body for ID-Only Operations (Step 6)
When an operation only needs an entity ID:
- Put the ID in the route: `PUT /api/agents/{id}/verify`
- Don't create a request DTO just to pass an ID
- Keeps the API surface clean and self-documenting

### 15. Public vs Protected Endpoints (Step 6)
- Read-only data that's public-facing → no auth attribute
- Mutations (toggle, update, delete) → `[Authorize]`
- Admin-only operations → `[Authorize(Roles = Roles.Admin)]`
Match auth to the data sensitivity, not the HTTP method alone.

---

## Suggestions for Future Projects

### 1. Standardize Error Code Format
Use `{entity}.{verb}` consistently (e.g., `user.notfound`, `agent.verified`).
Avoid mixed formats like `USER_NOT_FOUND` alongside `user.notfound`.

### 2. Separate Request/Response DTOs
Split into `Dtos/` subfolder when file exceeds ~100 lines.
Keep `Dtos.cs` for small domains.

### 3. Unit-of-Work
Use `SaveChangesAsync()` as implicit unit of work.
Only add explicit transactions for multi-step atomicity.

### 4. Dedicated Admin Service
When admin operations exceed ~10 endpoints, consider `IAdminService`
for cross-domain orchestration.

### 5. Testability
Services depend on interfaces (`IAppDbContext`, `UserManager<T>`).
Write integration tests with in-memory database.

### 6. API Versioning
Start with `api/v1/users` in future projects.
Costs nothing now, saves pain later.

### 7. Toggle Endpoints Over Partial Updates
For boolean fields (verified, featured, active), prefer a dedicated toggle endpoint
over a partial update. It's simpler, requires no request body, and the client
doesn't need to track current state.

### 8. Summary DTOs for Rich Endpoints
When an endpoint needs to return an entity plus related counts/stats, create a
dedicated summary DTO. Don't bloat the base DTO with optional count fields.
The base DTO stays fast for list views; the summary DTO is used only for detail views.

### 9. Parallel Count Queries
When counting multiple related entities, run separate `CountAsync()` calls.
EF Core translates each to an efficient SQL COUNT. Avoid loading entities into
memory just to count them with `.Count()` on a list.
