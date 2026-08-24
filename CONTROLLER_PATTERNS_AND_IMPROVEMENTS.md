# Controller Patterns & Improvements

## Pattern Used in PIPDC: Thin Controller Pattern

### 1. Primary Constructor DI

```csharp
public class DevelopmentProjectsController(IDevelopmentProjectService projectService) : ControllerBase
```

Service injected directly into the class declaration. No constructor body.

### 2. Single `CurrentUserId` Property

```csharp
private string CurrentUserId => User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;
```

Extracted once, reused across all endpoints.

### 3. `[FromQuery]` for List Params, `[FromBody]` for Mutations

```csharp
[HttpGet]
public async Task<IActionResult> GetAll([FromQuery] QueryParams queryParams, CancellationToken ct)

[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateRequest request, CancellationToken ct)
```

Query parameters bound from URL, request bodies from JSON.

### 4. Consistent Result → IActionResult Mapping

```csharp
var result = await service.DoSomethingAsync(..., ct);
return result.ToActionResult();
```

Every response goes through `ToActionResult()` extension. No manual `Ok()`, `BadRequest()`, `NotFound()` calls.

### 5. `CreatedAtAction` for POST

```csharp
return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
```

Returns 201 + Location header pointing to the newly created resource.

### 6. Role-Based Auth at Class Level

```csharp
[Authorize(Roles = "Admin")]  // entire class is admin-only
[Authorize]                   // entire class requires any authenticated user
```

No per-endpoint `[Authorize]` when the entire controller has the same access level.

### 7. Scoped Child Routes

```csharp
[Route("api/development-projects/{projectId:int}/units")]
[Route("api/development-projects/{projectId:int}/updates")]
```

Units and Updates nested under their parent project in the URL.

### 8. CancellationToken on Every Method

Every action method accepts `ct`. Propagated to the service call.

---

## Improvements for Next Project

### 1. Base Controller for Auth Extraction

Currently `User.FindFirstValue(...)` is repeated in every controller. Create a base controller:

```csharp
[ApiController]
public abstract class AuthBaseController : ControllerBase
{
    protected string CurrentUserId => User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;
    protected IList<string> CurrentUserRoles => User.FindAll("role").Select(c => c.Value).ToList();
}

[ApiController]
public abstract class AdminBaseController : AuthBaseController { }
```

### 2. Global Validation Filter

Currently validation is in DTO attributes (`[Required]`, `[MaxLength]`). Add a global validation filter or FluentValidation pipeline to reject invalid requests before they reach the controller.

### 3. Structured Error Responses

Currently `Result.ToActionResult()` maps generically. Define structured error response types (e.g., `ProblemDetails`) for consistent API error contracts:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "errors": { "code": "development.notfound" }
}
```

### 4. Controller-Level Logging

Add `ILogger<T>` to controllers for request/response logging. Currently logging only exists in services.

### 5. API Versioning

Use `[ApiVersion("1.0")]` from day one — easier to evolve the API later without breaking existing clients.

### 6. Response Caching

Add `[ResponseCache]` for public read endpoints (browse, detail) to reduce DB load on frequently accessed data.

### 7. Rate Limiting

Apply `[RateLimit]` to public endpoints (browse) to prevent abuse. ASP.NET Core 7+ has built-in rate limiting middleware.

### 8. Swagger/OpenAPI Documentation

Add `[ProducesResponseType]` attributes for clear Swagger/OpenAPI documentation:

```csharp
[ProducesResponseType(typeof(DevelopmentProjectDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
```

### 9. Base Controllers for DRY Auth

Create role-specific base controllers to DRY up repeated auth extraction across controllers that share the same access pattern.

---

## Summary Table

| Area | Current | Improvement |
|---|---|---|
| DI | Primary constructor | Explicit fields (testability) |
| Auth extraction | `User.FindFirstValue(...)` repeated | Base controller with `CurrentUser` / `CurrentUserRoles` |
| Validation | DTO attributes | FluentValidation + global filter |
| Error responses | Generic `ToActionResult()` | Structured `ProblemDetails` |
| Logging | Service-level only | Controller-level `ILogger<T>` |
| API versioning | None | `[ApiVersion("1.0")]` from day one |
| Response caching | None | `[ResponseCache]` on public reads |
| Rate limiting | None | `[RateLimit]` on public endpoints |
| Swagger docs | Implicit | `[ProducesResponseType]` attributes |
| Base controllers | None | `AdminBaseController`, `AuthBaseController` |
