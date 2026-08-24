# Phase 4 — Property Development Model: Implementation Plan

## Overview

Introduce a separate domain for property development projects (under-construction estates, phased developments) that is **completely independent** from the existing `Property` entity. No enums or statuses are shared.

---

## Step 1 — Domain Enums

Create 3 new files in `src/Domain/Enums/`:

### `DevelopmentProjectStatus.cs`
```
Planned, UnderConstruction, NearCompletion, Completed, OnHold
```

### `DevelopmentUnitStatus.cs`
```
Available, Reserved, Sold, UnderConstruction
```

### `DevelopmentTrackingStatus.cs`
```
Following, Stopped
```

---

## Step 2 — Domain Entities

Create 5 new files in `src/Domain/Entities/`.

### `DevelopmentProject : AuditableEntity`

| Property | Type | Notes |
|---|---|---|
| Name | `string` | required, max 200 |
| Description | `string` | required, max 4000 |
| Slug | `string` | required, unique, max 200 |
| Location | `string` | required, max 500 |
| Developer | `string?` | nullable, max 200 |
| Status | `DevelopmentProjectStatus` | stored as string |
| ExpectedCompletionDate | `DateTime?` | nullable |
| ProgressPercentage | `int` | 0–100, default 0 |
| Featured | `bool` | default false |

Navigations:
- `ICollection<DevelopmentUnit> Units`
- `ICollection<DevelopmentUpdate> Updates`
- `ICollection<DevelopmentProjectImage> Images`
- `ICollection<DevelopmentTracking> TrackedBy`

### `DevelopmentUnit : AuditableEntity`

| Property | Type | Notes |
|---|---|---|
| DevelopmentProjectId | `int` | FK |
| UnitIdentifier | `string` | required, max 50 |
| UnitType | `string` | required, max 100 |
| Status | `DevelopmentUnitStatus` | stored as string |
| Price | `decimal?` | nullable |
| Currency | `string` | default "NGN", max 10 |
| Description | `string?` | optional, max 2000 |

Navigations:
- `DevelopmentProject Project`
- `ICollection<DevelopmentTracking> TrackedBy`

### `DevelopmentUpdate : AuditableEntity`

| Property | Type | Notes |
|---|---|---|
| DevelopmentProjectId | `int` | FK |
| Title | `string` | required, max 200 |
| Description | `string` | required, max 4000 |
| ProgressPercentage | `int?` | optional snapshot |
| UpdateDate | `DateTime` | when update is dated |
| ImageUrls | `List<string>` | PostgreSQL text[] |

Navigations:
- `DevelopmentProject Project`

### `DevelopmentProjectImage : BaseEntity`

| Property | Type | Notes |
|---|---|---|
| DevelopmentProjectId | `int` | FK |
| Url | `string` | required, max 500 |
| PublicId | `string` | required, max 200 |
| IsCover | `bool` | default false |
| DisplayOrder | `int` | default 0 |

Navigations:
- `DevelopmentProject Project`

### `DevelopmentTracking : BaseEntity`

| Property | Type | Notes |
|---|---|---|
| UserId | `string` | FK → AppUser, max 450 |
| DevelopmentProjectId | `int` | FK |
| DevelopmentUnitId | `int?` | FK, nullable |
| Status | `DevelopmentTrackingStatus` | stored as string, default Following |

Navigations:
- `AppUser User`
- `DevelopmentProject Project`
- `DevelopmentUnit? Unit`

Unique index on `(UserId, DevelopmentProjectId)`.

---

## Step 3 — DbContext & Configurations

### Modify `src/Application/Data/IAppDbContext.cs`
Add 5 new DbSet properties.

### Modify `src/Infrastructure/Data/AppDbContext.cs`
Add 5 new DbSet properties.

### Create 5 configuration files in `src/Infrastructure/Data/Configurations/`

**`DevelopmentProjectConfiguration`**
- Unique index on `Slug`
- String max lengths on Name, Description, Slug, Location, Developer
- Indexes on `Status`, `Featured`

**`DevelopmentUnitConfiguration`**
- FK → DevelopmentProject with `Cascade`
- Unique composite index `(DevelopmentProjectId, UnitIdentifier)`
- String max lengths

**`DevelopmentUpdateConfiguration`**
- FK → DevelopmentProject with `Cascade`
- Index on `(DevelopmentProjectId, UpdateDate)`

**`DevelopmentProjectImageConfiguration`**
- FK → DevelopmentProject with `Cascade`
- Composite index `(DevelopmentProjectId, DisplayOrder)`

**`DevelopmentTrackingConfiguration`**
- FK → DevelopmentProject with `Cascade`
- FK → AppUser with `Restrict`
- FK → DevelopmentUnit with `SetNull`
- Unique index `(UserId, DevelopmentProjectId)`
- Index on `UserId`

---

## Step 4 — Application DTOs

Create `src/Application/Developments/Dtos.cs`.

### Request DTOs

- `CreateDevelopmentProjectRequest(Name, Description, Slug?, Location, Developer?, Status?, ExpectedCompletionDate?, ProgressPercentage?, Featured?, Images[])`
- `UpdateDevelopmentProjectRequest(same shape, Status required)`
- `CreateDevelopmentUnitRequest(UnitIdentifier, UnitType, Status?, Price?, Currency?, Description?)`
- `UpdateDevelopmentUnitRequest(UnitIdentifier, UnitType, Status, Price?, Currency?, Description?)`
- `CreateDevelopmentUpdateRequest(Title, Description, ProgressPercentage?, UpdateDate?, ImageUrls[])`
- `UpdateDevelopmentUpdateRequest(Title, Description, ProgressPercentage?, UpdateDate?)`

### Response DTOs

- `DevelopmentProjectDto(Id, Name, Slug, Description, Location, Developer?, Status, ExpectedCompletionDate?, ProgressPercentage, Featured, Images[], UnitCount, UpdateCount, CreatedAt, UpdatedAt?)`
- `DevelopmentUnitDto(Id, UnitIdentifier, UnitType, Status, Price?, Currency?, Description?, CreatedAt, UpdatedAt?)`
- `DevelopmentUpdateDto(Id, Title, Description, ProgressPercentage?, UpdateDate, ImageUrls[], CreatedAt, UpdatedAt?)`
- `DevelopmentTrackingDto(Id, DevelopmentProjectId, DevelopmentProjectName, DevelopmentUnitId?, DevelopmentUnitIdentifier?, Status, TrackedAt)`
- `DevelopmentProjectDetailDto extends DevelopmentProjectDto with Units[], Updates[]`

### Query Params

- `DevelopmentProjectQueryParameters { Keyword?, Status?, Featured?, PageNumber=1, PageSize=10 }`

---

## Step 5 — Application Services (Interfaces)

Create 5 interface files in `src/Application/Developments/`.

### `IDevelopmentProjectService` (admin)
```
GetAllAsync(q, ct) → PaginatedResult<DevelopmentProjectDto>
GetByIdAsync(id, ct) → DevelopmentProjectDetailDto
CreateAsync(request, ct) → DevelopmentProjectDto
UpdateAsync(id, request, ct) → DevelopmentProjectDto
DeleteAsync(id, ct) → Result
UpdateFeaturedAsync(id, featured, ct) → Result
```

### `IDevelopmentUnitService` (admin)
```
GetByProjectAsync(projectId, ct) → IReadOnlyList<DevelopmentUnitDto>
CreateAsync(projectId, request, ct) → DevelopmentUnitDto
UpdateAsync(projectId, unitId, request, ct) → DevelopmentUnitDto
DeleteAsync(projectId, unitId, ct) → Result
```

### `IDevelopmentUpdateService` (admin)
```
GetByProjectAsync(projectId, ct) → IReadOnlyList<DevelopmentUpdateDto>
CreateAsync(projectId, request, ct) → DevelopmentUpdateDto
UpdateAsync(projectId, updateId, request, ct) → DevelopmentUpdateDto
DeleteAsync(projectId, updateId, ct) → Result
```

### `IDevelopmentTrackingService` (client)
```
GetTrackedAsync(userId, q, ct) → PaginatedResult<DevelopmentTrackingDto>
TrackAsync(userId, projectId, unitId?, ct) → Result
StopTrackingAsync(userId, projectId, ct) → Result
IsTrackingAsync(userId, projectId, ct) → bool
```

### `IDevelopmentProjectPublicService` (public read)
```
GetPublicAllAsync(q, ct) → PaginatedResult<DevelopmentProjectDto>
GetPublicBySlugAsync(slug, ct) → DevelopmentProjectDetailDto
GetPublicByIdAsync(id, ct) → DevelopmentProjectDetailDto
```

---

## Step 6 — Service Implementations

Create 5 implementation files in `src/Application/Developments/` (same folder as interfaces).

Services that use `IAppDbContext` for business logic belong in the Application layer, not Infrastructure. Infrastructure is reserved for external provider implementations (email, auth/JWT).

Follow existing patterns:
- Constructor injection of `IAppDbContext`, `ILogger<T>`
- Return `Result<T>` / `Result` from domain
- Use `ProjectTo<TDto>()` or manual mapping
- Validate entity existence before operations
- Use `SlugGenerator`-style slug creation from Name

---

## Step 7 — API Controllers

Create 4 controllers in `src/API/Controllers/`.

### `DevelopmentProjectsController`
`[Route("api/development-projects")]` `[Authorize(Roles = "Admin")]`

| Method | Route | Description |
|---|---|---|
| GET | `` | List (paged, filtered) |
| GET | `{id:int}` | Detail with units + updates |
| POST | `` | Create |
| PUT | `{id:int}` | Update |
| DELETE | `{id:int}` | Delete |
| PUT | `{id:int}/featured` | Toggle featured |

### `DevelopmentUnitsController`
`[Route("api/development-projects/{projectId:int}/units")]` `[Authorize(Roles = "Admin")]`

| Method | Route | Description |
|---|---|---|
| GET | `` | List units for project |
| POST | `` | Create unit |
| PUT | `{unitId:int}` | Update unit |
| DELETE | `{unitId:int}` | Delete unit |

### `DevelopmentUpdatesController`
`[Route("api/development-projects/{projectId:int}/updates")]` `[Authorize(Roles = "Admin")]`

| Method | Route | Description |
|---|---|---|
| GET | `` | List updates for project |
| POST | `` | Create update |
| PUT | `{updateId:int}` | Update |
| DELETE | `{updateId:int}` | Delete |

### `DevelopmentProjectsPublicController`
`[Route("api/development-projects")]` (no auth required)

| Method | Route | Description |
|---|---|---|
| GET | `browse` | Public listing (paged) |
| GET | `browse/{slug}` | Public detail by slug |
| GET | `browse/{id:int}` | Public detail by id |

### `DevelopmentTrackingController`
`[Route("api/development-tracking")]` `[Authorize]`

| Method | Route | Description |
|---|---|---|
| GET | `` | List my tracked projects |
| POST | `` | Track project (body: projectId, unitId?) |
| DELETE | `{projectId:int}` | Stop tracking |

---

## Step 8 — Seeder

Add to `src/Infrastructure/Data/DevelopmentSeeder.cs`:
- 2–3 DevelopmentProject records (Jos/Plateau locations, mixed statuses)
- 2–3 DevelopmentUnit records per project
- 1–2 DevelopmentUpdate records per project
- Cover images for each project

---

## Step 9 — Build & Verify

- `dotnet build PIPDC.csproj` — 0 errors, 0 warnings
- Migration generates cleanly

---

## Files Summary

| Action | Files |
|---|---|
| New entity files | 5 |
| New enum files | 3 |
| New configuration files | 5 |
| New DTOs file | 1 |
| New service interfaces | 5 |
| New service implementations | 5 |
| New controllers | 5 |
| Modified: IAppDbContext | 1 |
| Modified: AppDbContext | 1 |
| Modified: DevelopmentSeeder | 1 |
| **Total** | **~33 files** |
