# PIPDC — Project Documentation

Comprehensive, source-verified documentation of the PIPDC real-estate platform as currently implemented.

> **Purpose of this document:** A single reference file that fully describes the system — its architecture, domain model, API surface, data layer, and frontend — so that a developer or an AI model can read it and understand exactly what exists in the codebase. Everything below was verified against the source code.

---

## 1. Project Overview

**PIPDC** (Plateau State Property Investment & Development Company) is a government-backed real-estate platform for Plateau State, Nigeria. It connects property seekers (clients) with real-estate agents, manages property listings, enquiries, saved properties, blog content, users, and agents through a web application.

The platform consists of **two codebases**:

| Piece | Location | Tech |
|---|---|---|
| Backend API | `C:\Users\OYALE\Desktop\Project\PIPDC` | ASP.NET Core (.NET 9) + EF Core + PostgreSQL |
| Frontend SPA | `C:\Users\OYALE\Desktop\Project\PIPDC-Frontend` | React 18 + Vite + TypeScript + Tailwind + TanStack Query |

The backend is a **single-project** web API (`PIPDC.csproj`) organized into `src/API`, `src/Application`, `src/Domain`, and `src/Infrastructure` folders (folder-layered, not separate projects). The frontend is a React SPA consuming the REST API via Axios.

---

## 2. Technology Stack

### Backend (`PIPDC.csproj`)

- **Runtime:** .NET 9 (`net9.0`), `Nullable` + `ImplicitUsings` enabled
- **Web:** ASP.NET Core MVC (controllers), `Microsoft.AspNetCore.OpenApi` 9.0.18, `Scalar.AspNetCore` 2.16.13
- **Identity:** `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 9.0.17 + `Microsoft.AspNetCore.Authentication.JwtBearer` 9.0.17
- **Real-time:** ASP.NET Core SignalR (included in the shared framework — no package reference), hub at `/hubs/messaging`; frontend uses `@microsoft/signalr` ^10.0.11
- **Data:** `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.4, `Microsoft.EntityFrameworkCore.Design` 9.0.17
- **EF tooling:** `dotnet-ef` 9.0.17 (installed via `.config/dotnet-tools.json`)
- **User secrets ID:** `e1fa52bf-603f-4f38-b75a-6670210bed0a`
- **HTTPS dev profile:** `https://localhost:7123;http://localhost:5123` (launch profile name `https`, env `Development`)

### Frontend (`package.json`)

- React `^18.3.1`, React DOM `^18.3.1`
- `react-router-dom` `^7.18.1` (`createBrowserRouter` / `RouterProvider`)
- `@tanstack/react-query` `^5.101.3` (server state)
- `axios` `^1.18.1` (HTTP client)
- `react-hook-form` `^7.82.0` + `zod` `^4.4.3` + `@hookform/resolvers` (forms)
- `tailwindcss` `^3.4.1` (styling), `framer-motion` `^12.42.2`, `lucide-react` `^0.344.0` (icons)
- `vite` `^5.4.2`, `typescript` `^5.5.3`
- Installed but **unused**: `@supabase/supabase-js`, `@heroicons/react`
- Scripts: `dev` (vite), `build` (vite build), `lint` (eslint), `typecheck` (`tsc --noEmit`), `preview`

---

## 3. System Architecture

### 3.1 Layered backend structure

```
src/
├── API/                     # ASP.NET Core Web API
│   ├── Program.cs           # Startup pipeline
│   ├── Controllers/         # 11 controllers (HTTP layer)
│   ├── Hubs/                # SignalR: MessagingHub, ConversationGroup, JwtSubUserIdProvider
│   └── Extensions/          # ResultExtensions, GlobalExceptionHandler
├── Application/             # Business logic layer
│   ├── Auth/                # Roles, JwtSettings, auth DTOs/interfaces
│   ├── Enquiries/           # Enquiry domain logic + DTOs
│   ├── Conversations/       # Conversation/Message logic, DTOs, projections, authorization
│   ├── Properties/          # Property domain logic + DTOs
│   ├── Agents/              # Agent domain logic + DTOs
│   ├── Blog/                # Blog domain logic + DTOs
│   ├── Dashboard/           # Role-based dashboard aggregation
│   ├── SavedProperties/     # Saved/favourite properties
│   ├── Users/               # Admin user management
│   ├── Data/                # IAppDbContext (persistence abstraction)
│   └── Common/              # PaginatedResult
├── Domain/                  # Entities + enums + common primitives
│   ├── Entities/            # AppUser, Agent, Property, Enquiry, …
│   ├── Auth/                # RefreshToken
│   ├── Common/              # Result, Error, BaseEntity, AuditableEntity
│   └── Enums/               # EnquiryStatus, PropertyStatus, …
└── Infrastructure/          # EF Core, Auth, Data access
    ├── Auth/                # AuthService, TokenService
    ├── Data/                # AppDbContext, configurations, seeders, migrations
    └── DependencyInjection.cs
```

### 3.2 Request flow

```
HTTP request
   → Controller (attribute routing + [Authorize])
   → Service (application logic, returns Result<T>)
   → IAppDbContext → EF Core → PostgreSQL
   ← Result<T> → ResultExtensions.ToActionResult() → HTTP response
```

Controllers are thin. All business rules live in services. Services return `Result<T>`/`Result` (never throw for expected failures). A single `GlobalExceptionHandler` catches unhandled exceptions.

### 3.3 Startup pipeline (`src/API/Program.cs`)

1. `AddControllers()`
2. `AddSignalR()` + `AddSingleton<IUserIdProvider, JwtSubUserIdProvider>()`
3. `AddExceptionHandler<GlobalExceptionHandler>()` + `AddProblemDetails()`
4. `AddOpenApi()` (dev) + Scalar API reference (dev)
5. `AddInfrastructure(config)` — Identity, JWT bearer, CORS `"AllowFrontend"`, EF/Postgres
6. `AddApplication()` — DI of application services
7. ApiBehaviorOptions override — model-validation failures return a `BadRequestObjectResult` wrapping an `Error` (code `validation.requestinvalid`)
8. On startup: `MigrateAsync()` (auto-applies migrations) → `RoleSeeder` → (dev only) `DevelopmentSeeder`
9. Middleware: `UseExceptionHandler` → `UseHttpsRedirection` → `UseCors` → `UseAuthentication` → `UseAuthorization` → `MapControllers` → `MapHub<MessagingHub>("/hubs/messaging")`

### 3.4 Real-time architecture (SignalR)

```
React Client
     │
     ├──────── REST ────────► ASP.NET Core API
     │                              │
     │                              ▼
     │                         Application
     │                              │
     │                              ▼
     │                         PostgreSQL
     │
     └──── SignalR ───────────────► MessagingHub
```

- The database remains the **source of truth**. SignalR is delivery-only: a message is first committed to PostgreSQL by `MessageService`, then a `NewMessage` event is published to the conversation group.
- Broadcast is **best-effort**: a SignalR delivery failure is logged (`ILogger`) and does not fail the already-persisted REST operation — the client can always refetch history via REST.
- Broadcasting lives in `MessageService` (via injected `IHubContext<MessagingHub>`), not in the Hub. The Hub only manages connections and groups.

---

## 4. Domain Model

### 4.1 Base classes

- `BaseEntity` (abstract): `int Id`, `DateTime CreatedAt`
- `AuditableEntity : BaseEntity` (abstract): adds `DateTime? UpdatedAt`

### 4.2 `AppUser : IdentityUser` (string PK)

- `FirstName`, `LastName`
- `FullName` — **computed**: `$"{FirstName} {LastName}"` (get-only; translates in EF projections but must NOT be used in `GROUP BY`)
- `CreatedAt` (default `DateTime.UtcNow`)
- Plus all Identity members (`Email`, `PasswordHash`, `PhoneNumber`, `LockoutEnd`, `AccessFailedCount`, etc.)
- Navigations: `Agent? Agent` (1-1), `ICollection<SavedProperty> SavedProperties`, `ICollection<AiChatSession> AiChatSessions`, `ICollection<Enquiry> Enquiries`, `ICollection<Conversation> Conversations` (as client), `ICollection<Message> Messages` (as sender), implicit property-creation + refresh-token collections

### 4.3 `Agent : AuditableEntity`

`Bio?`, `Title?`, `PhotoUrl?`, `AgencyName`, `LicenseNumber?`, `PhoneNumber`, `IsVerified`, `UserId` (FK → AppUser).
Navigations: `AppUser User` (1-1, Restrict), `ICollection<Property> Properties` (1-N, Restrict), `ICollection<Conversation> Conversations` (1-N, Restrict).

### 4.4 `Property : AuditableEntity`

`Title`, `Description`, `Slug` (unique), `Price` (decimal), `Currency` (default `"NGN"`), `Period?`, `Status` (PropertyStatus), `PropertyType`, `ListingType`, `Bedrooms?`, `Bathrooms?`, `Size?`, `SizeUnit` (default `"sqm"`), `LotSize?`, `YearBuilt?`, `Amenities` (`List<string>`), `Address`, `State`, `City`, `Area?`, `Latitude?`, `Longitude?`, `Featured`, `AgentId` (FK → Agent), `CreatedByUserId` (FK → AppUser).
Navigations: `Agent Agent` (Restrict), `AppUser CreatedByUser` (Restrict), `ICollection<PropertyImage> PropertyImages` (Cascade), `ICollection<Enquiry> Enquiries` (Restrict), `ICollection<SavedProperty> SavedByUsers` (Cascade), `SaleRecord?`, `LeaseRecord?` (1-1, Restrict).

### 4.5 `PropertyImage : BaseEntity`

`Url`, `PublicId`, `IsCover`, `DisplayOrder`, `PropertyId` (FK, Cascade).

### 4.6 `Enquiry : AuditableEntity`

`FullName`, `Email`, `Phone?`, `Message`, `Status` (EnquiryStatus), `PropertyId` (FK), `UserId?` (FK), **`AgentReadAt?`** — UTC timestamp of the last time the assigned agent opened the enquiry; `null` = unread.
Navigations: `Property Property` (Restrict), `AppUser? User` (SetNull), `Conversation? Conversation` (1-1, Cascade).

### 4.7 `Conversation : AuditableEntity`

`EnquiryId` (FK → Enquiry, **unique**), `ClientUserId` (FK → AppUser), `AgentId` (FK → Agent), **`LastMessageAt?`** — UTC timestamp of the most recent message; `null` until the first message is sent (orders conversation lists).
Navigations: `Enquiry Enquiry` (1-1, Cascade), `AppUser Client` (Restrict), `Agent Agent` (Restrict), `ICollection<Message> Messages` (Cascade).

> **Creation rule:** a `Conversation` only comes into existence when the first message is successfully sent (atomic `Conversation + Message + LastMessageAt` in a single `SaveChanges`). Opening the messaging UI never creates one. The unique `EnquiryId` index enforces one conversation per enquiry at the database level.

### 4.8 `Message : BaseEntity`

`ConversationId` (FK → Conversation, Cascade), `SenderUserId` (FK → AppUser, Restrict), `Content` (max 4000), **`ReadAt?`** — UTC timestamp of the first time the recipient read the message; `null` = unread.
Navigations: `Conversation Conversation`, `AppUser Sender`.

### 4.9 `SavedProperty : BaseEntity`

`UserId`, `PropertyId`. Unique index `(UserId, PropertyId)`. Both FKs Cascade.

### 4.10 `SaleRecord : AuditableEntity`

`SalePrice`, `SaleDate`, `BuyerName`, `BuyerContact`, `Notes?`, `PropertyId` (1-1, Restrict).

### 4.11 `LeaseRecord : AuditableEntity`

`TenantName`, `TenantContact`, `MonthlyRent`, `LeaseStartDate`, `LeaseEndDate`, `Notes?`, `PropertyId` (1-1, Restrict).

### 4.12 `BlogPost : AuditableEntity`

`Title`, `Slug` (unique), `Content`, `Excerpt?`, `CoverImageUrl?`, `CoverImagePublicId?`, `Status` (BlogPostStatus), `PublishedAt?`. No navigations.

### 4.13 `AiChatSession : BaseEntity`

`Title?`, `MessagesJson`, `LastMessageAt`, `UserId` (FK, Cascade). (Schema exists; **no service/controller currently uses it** — reserved for the future AI phase.)

### 4.14 `RefreshToken : BaseEntity` (`src/Domain/Auth`)

`Token` (unique), `UserId` (FK, Cascade), `Expires`, `Revoked?`, `ReplacedByToken?`. Computed `IsActive => Revoked is null && UtcNow < Expires`.

### 4.15 Relationship diagram (key relationships)

```
AppUser (Identity, string PK)
   ├── 1-1 Agent (Agent.UserId, Restrict)
   ├── 1-N SavedProperty (Cascade)
   ├── 1-N AiChatSession (Cascade)
   ├── 1-N Enquiry (SetNull)
   ├── 1-N Conversation as Client (ClientUserId, Restrict)
   ├── 1-N Message as Sender (SenderUserId, Restrict)
   ├── 1-N Property as CreatedByUser (Restrict)
   └── 1-N RefreshToken (Cascade)

Agent (int PK)
   ├── 1-N Property (AgentId, Restrict)
   └── 1-N Conversation (AgentId, Restrict)

Property (int PK)
   ├── 1-N PropertyImage (Cascade)
   ├── 1-N Enquiry (Restrict)
   ├── 1-N SavedProperty (Cascade)
   ├── 1-1 SaleRecord (Restrict)
   └── 1-1 LeaseRecord (Restrict)

Enquiry → Property (N-1, Restrict) → Agent → AppUser  (agent chain for enquiries)

Conversation (int PK)
   ├── 1-1 Enquiry (EnquiryId, unique, Cascade)
   ├── 1-N Message (ConversationId, Cascade)
   ├── 1-1 AppUser as Client (ClientUserId, Restrict)
   └── 1-1 Agent (AgentId, Restrict)

Message (int PK)
   ├── 1-1 Conversation (ConversationId, Cascade)
   └── 1-1 AppUser as Sender (SenderUserId, Restrict)
```

### 4.16 Enums

| Enum | Members |
|---|---|
| `EnquiryStatus` | `Pending, InProgress, ViewingScheduled, Resolved` |
| `PropertyStatus` | `Available, Pending, Sold, Leased, Withdrawn` |
| `PropertyType` | `Residential, Commercial, Land, Industrial, Mixed, DetachedHouse, SemiDetached, Terrace, Apartment, Penthouse, Villa, Mansion, Townhouse` |
| `ListingType` | `ForSale, ForLease` |
| `BlogPostStatus` | `Draft, Published, Archived` |
| `ErrorType` | `Failure, NotFound, Validation, Conflict, Unauthorized, Forbidden` |

Domain enums are persisted as **strings** (`HasConversion<string>()`). `ErrorType` is internal-only.

---

## 5. Authentication & Authorization

### 5.1 Roles (`src/Application/Auth/Roles.cs`)

- `Admin`, `Agent`, `User` (used both in `[Authorize(Roles=…)]` and as claim values).

### 5.2 JWT access tokens (`TokenService.CreateAccessToken`)

- **Claims:** `sub` = user id, `email`, `name` = full name, `jti` = new Guid, and one `"role"` claim per role.
- **Expiry:** `AccessTokenMinutes` = **30 minutes** (config).
- **Signing:** symmetric `HmacSha256` with key = `UTF8.GetBytes(JwtSettings.Key)`; token written via `JsonWebTokenHandler`.
- Issuer/audience come from the token descriptor (config below).

### 5.3 JWT bearer validation (`Infrastructure/DependencyInjection.cs`)

```csharp
MapInboundClaims = false;   // claim types stay exactly as written (e.g. "sub", "role")
TokenValidationParameters = {
    ValidateIssuer = true, ValidateAudience = true,
    ValidateLifetime = true, ValidateIssuerSigningKey = true,
    ValidIssuer   = JwtSettings.Issuer,     // https://pipdc.plateaustate.gov.ng
    ValidAudience = JwtSettings.Audience,   // pipdc-api
    IssuerSigningKey = new SymmetricSecurityKey(UTF8 bytes of JwtSettings.Key),
    ClockSkew = TimeSpan.Zero,
    NameClaimType = "name",                 // User.Identity.Name resolves from "name"
    RoleClaimType = "role"
}
```

Because `MapInboundClaims = false`, controllers read the user id from `User.FindFirstValue(JwtRegisteredClaimNames.Sub)` (`"sub"`) and roles from `User.FindAll("role")`.

### 5.4 App settings (JWT section in `appsettings.json`)

| Key | Default value |
|---|---|
| `JwtSettings:Key` | *(empty — supplied via user secrets / env)* |
| `JwtSettings:Issuer` | `https://pipdc.plateaustate.gov.ng` |
| `JwtSettings:Audience` | `pipdc-api` |
| `JwtSettings:AccessTokenMinutes` | `30` |
| `JwtSettings:RefreshTokenDays` | `7` |

### 5.5 Refresh tokens & rotation (`AuthService`)

- Refresh token = 64 random bytes, Base64, opaque; persisted in `RefreshTokens`; lives **7 days**.
- `POST /auth/refresh` **rotates**: revokes the presented token, persists a new one, links via `ReplacedByToken`.
- **Reuse detection:** if an already-revoked token is presented again, ALL active tokens for that user are revoked (session takeover protection).
- `POST /auth/revoke` marks the presented token revoked (used by frontend sign-out).
- `ForgotPasswordAsync` currently returns success but **does nothing** (email delivery intentionally not implemented yet).

### 5.6 Registration & role assignment rules

- Public `POST /auth/register` **always** assigns only `Roles.User`.
- Admin-only `POST /auth/add-role`: adding `Agent` also auto-creates an `Agent` row (`Title="Agent"`, `AgencyName="PIPDC Agency"`, `IsVerified=false`) if none exists — transactional.
- Admin-only `POST /auth/remove-role`: cannot remove the **last Admin**; when removing `Agent`, the user's properties are **transferred to an admin agent** (`GetOrCreateAdminAgentAsync`, `Title="Administrator"`, `AgencyName="PIPDC Administration"`) and the agent row is deleted — transactional.

### 5.7 CORS

Single policy `"AllowFrontend"`: if `Cors:AllowedOrigins` is non-empty → `WithOrigins(...)`, `AllowAnyHeader`, `AllowAnyMethod`, `AllowCredentials`. Development config allows `http://localhost:5173`; empty array → permissive dev fallback.

### 5.8 SignalR authentication (`/hubs/messaging`)

- The hub is `[Authorize]`; anonymous connections are rejected.
- Browsers cannot set the `Authorization` header on WebSocket, so the **JWT is extracted from the `access_token` query parameter** (`OnMessageReceived` in `Infrastructure/DependencyInjection.cs`) — **only** for the path `/hubs/messaging`. Query extraction is scoped to that exact path so no other endpoint trusts query-string tokens.
- Token validation is unchanged (same `TokenValidationParameters`, key, issuer, audience).
- **`JwtSubUserIdProvider`** (`IUserIdProvider` singleton) maps each authenticated principal to `User.FindFirstValue(JwtRegisteredClaimNames.Sub)`, so `Context.UserIdentifier` equals the REST `sub` user id. This lets the same connection be shared by both client and agent sessions (roles come from `Context.User` claims, never from the client).

---

## 6. API Reference

Base path: `/api`. All endpoints return the documented DTOs. List endpoints return `PaginatedResult<T>`:

```jsonc
{ "items": [...], "pageNumber": 1, "pageSize": 10,
  "totalCount": 0, "totalPages": 0, "hasPreviousPage": false, "hasNextPage": false }
```

### 6.1 Auth — `api/auth`

| Method | Route | Access | Purpose |
|---|---|---|---|
| POST | `/api/auth/register` | Anonymous | Register (role `User`) |
| POST | `/api/auth/login` | Anonymous | Login → `AuthResponse` |
| POST | `/api/auth/refresh` | Anonymous | Rotate refresh token → new `AuthResponse` |
| POST | `/api/auth/revoke` | Anonymous | Revoke refresh token |
| POST | `/api/auth/forgot-password` | Anonymous | No-op (returns success) |
| GET | `/api/auth/me` | `[Authorize]` | Current user profile |
| PUT | `/api/auth/me` | `[Authorize]` | Update own profile |
| POST | `/api/auth/add-role` | Admin | Add role (+ auto-create agent if `Agent`) |
| POST | `/api/auth/remove-role` | Admin | Remove role (transfers properties on Agent removal) |

**DTOs:**
- `AuthResponse(string UserId, string Email, IEnumerable<string> Roles, string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt)`
- `CurrentUserDto(string Id, string Email, string FirstName, string LastName, string? PhoneNumber, string FullName, IEnumerable<string> Roles)`
- `RegisterRequest(FirstName*, LastName*, Email*, Password*)` — password min 8
- `LoginRequest(Email*, Password*)`
- `UpdateProfileRequest(FirstName*, LastName*, PhoneNumber?)`

### 6.2 Properties — `api/properties`

| Method | Route | Access | Purpose |
|---|---|---|---|
| GET | `/api/properties` | Anonymous | Paginated list + filters |
| GET | `/api/properties/featured` | Anonymous | Featured (max 6) |
| GET | `/api/properties/slug/{slug}` | Anonymous | By slug |
| GET | `/api/properties/{id:int}` | Anonymous | By id |
| GET | `/api/properties/{id:int}/similar` | Anonymous | Same type, max 3 |
| POST | `/api/properties` | Agent, Admin | Create (agent auto-assigned; admin chooses `AgentId`) |
| PUT | `/api/properties/{id:int}` | Agent, Admin | Update (ownership enforced) |
| PUT | `/api/properties/{id:int}/featured` | Admin | Toggle featured |
| DELETE | `/api/properties/{id:int}` | Agent, Admin | Delete (ownership; blocked if sale/lease record exists) |

**`PropertyDto`** fields: `Id, Title, Slug, Description, Price, Currency, Period, Status, Type, PropertyType, ListingType, Bedrooms, Bathrooms, Size, SizeUnit, LotSize, YearBuilt, Address, City, Area, State, Latitude, Longitude, Images, CoverImage, Amenities, Featured, AgentId, AgentName, AgentPhoto, IsSaved, EnquiryCount, CreatedAt, UpdatedAt`.
- `Status` is the **frontend label** (`"For Sale"`, `"For Lease"`, `"Sold"`, `"Off Market"`); `Type` is the frontend property-type label; `PropertyType`/`ListingType` are enum `.ToString()` values.
- `EnquiryCount` = real `Enquiries.Count()` for the property (computed per request; grouped queries used in saved-property lists to avoid N+1).

**Filter query params (`PropertyQueryParameters`):** `Query`, `Keyword`, `Location`, `City`, `State`, `Type`, `PropertyType`, `ListingType`, `Status`, `AgentId`, `Sort` (`price-asc|price-desc|popular`), `SortBy` (`price|title`), `SortDescending` (default `true`), `MinPrice`, `MaxPrice`, `Bedrooms`, `Bathrooms`, `Page`/`PageNumber` (default 1), `PageSize` (default 10, max 100).
Status filter mapping: `"For Sale"` → `(Available|Pending) & ForSale`; `"For Lease"` → `(Available|Pending) & ForLease`; `"Sold"` → `Sold`; `"Off Market"` → `Withdrawn|Leased`; else raw `PropertyStatus` parse.

### 6.3 Enquiries — `api/enquiries`

| Method | Route | Access | Purpose |
|---|---|---|---|
| POST | `/api/enquiries` | `[Authorize]` | Create enquiry; identity derived from authenticated user |
| GET | `/api/enquiries` | Agent, Admin | List (agents see only their own properties' enquiries) |
| GET | `/api/enquiries/mine` | `[Authorize]` | The caller's own enquiries |
| GET | `/api/enquiries/{id:int}` | Agent, Admin | Detail (**marks `AgentReadAt` for non-admin readers**) |
| PUT | `/api/enquiries/{id:int}` | Agent, Admin | Update incl. status (ownership enforced) |
| DELETE | `/api/enquiries/{id:int}` | Agent, Admin | Delete (ownership enforced) |
| GET | `/api/enquiries/agents/summary` | Admin | Per-agent aggregates |
| GET | `/api/enquiries/agents/{agentId:int}` | Admin | Enquiries for one agent's properties |
| POST | `/api/enquiries/{id:int}/notify-agent` | Admin | Returns `AgentNotifyResultDto` (**no email sent yet**) |

**DTOs:**
- `EnquiryDto(int Id, string FullName, string Email, string? Phone, string Message, string Status, int PropertyId, string PropertyTitle, string PropertySlug, string? UserId, int AgentId, string AgentName, DateTime? AgentReadAt, bool IsRead, DateTime CreatedAt, DateTime? UpdatedAt)` — `IsRead = AgentReadAt != null`
- `AgentEnquirySummaryDto(int AgentId, string AgentName, int TotalEnquiries, int UnreadEnquiries, DateTime? LatestEnquiryAt)`
- `AgentNotifyResultDto(int EnquiryId, string EnquiryStatus, string ClientFullName, string ClientEmail, string? ClientPhone, string ClientMessage, int AgentId, string AgentName, string AgentEmail, int PropertyId, string PropertyTitle, string PropertySlug, DateTime? AgentReadAt)`
- `CreateEnquiryRequest(Message*, PropertyId*)` — `FullName`/`Email`/`Phone` are **populated server-side** from the authenticated `AppUser` (never from the request body)
- `UpdateEnquiryRequest(FullName*, Email*, Phone?, Message*, Status*)`

**Filter query params (`EnquiryQueryParameters`):** `Keyword` (FullName/Email/Message), `Status`, `PropertyId`, `SortBy` (`status` or default CreatedAt), `SortDescending` (default `true`), `PageNumber`, `PageSize` (default 10, max 100).

**Agent summaries logic:** groups enquiries by `e.Property.AgentId` server-side (`TotalEnquiries = Count()`, `UnreadEnquiries = Count(e.AgentReadAt == null)`, `LatestEnquiryAt = Max(e.CreatedAt)`), loads agents with their users in a second query, resolves `AgentName` **in memory** (never in the SQL group — avoids the `AppUser.FullName` translation failure). Sort: `unread` (UnreadEnquiries), `name` (AgentName), or default latest activity; `SortDescending` honored; paginated in memory.

**Read tracking:** `GET /api/enquiries/{id}` sets `AgentReadAt = UtcNow` only when the viewer is **not** Admin (i.e. an agent) and the value is currently null. Admins and clients never mutate it.

**Authorization:** Admin can manage any enquiry; an Agent can manage enquiries only for properties whose `AgentId` matches the agent linked to their account (`enquiry.forbidden` otherwise). Clients only access `mine`.

### 6.4 Conversations — `api/conversations` (`[Authorize]` class-level)

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/conversations` | Paginated conversations the caller participates in (Admin sees all) |
| GET | `/api/conversations/{id:int}` | Conversation detail (client-own, managing agent, or Admin) |
| GET | `/api/enquiries/{enquiryId:int}/conversation` | Read-only messaging state for an enquiry: `EnquiryConversationStateDto { enquiryId, conversation|null, client, agent, property }`. **Never creates a conversation.** |

**`ConversationDto`:** `Id, EnquiryId, Client (ConversationClientDto), Agent (ConversationAgentDto), Property (ConversationPropertyDto), LastMessageAt?, MessageCount, UnreadCount, CreatedAt, UpdatedAt`.
- `UnreadCount` = messages where `SenderUserId != currentUserId && ReadAt == null` (per-caller).
- `ConversationClientDto(UserId, FullName, Email)`; `ConversationAgentDto(AgentId, FullName, AgencyName, PhotoUrl?)`; `ConversationPropertyDto(PropertyId, Title, Slug)`.
- `ConversationQueryParameters`: `PageNumber` (default 1), `PageSize` (default 10, max 100). Sorted by `LastMessageAt` descending (nulls last).

**Authorization** (`ConversationAuthorization`): the client who owns it (`ClientUserId == currentUserId`), the agent who manages the conversation's property (`Property.AgentId` → current agent), or Admin. Merely knowing an id grants no access — otherwise `conversation.forbidden`.

### 6.5 Messages — `api/conversations/{conversationId:int}/messages` (`[Authorize]` class-level)

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/conversations/{conversationId:int}/messages` | Full history, oldest → newest (`IReadOnlyList<MessageDto>`) |
| POST | `/api/conversations/{conversationId:int}/messages` | Send; 201 + `MessageDto` |
| POST | `/api/conversations/{conversationId:int}/messages/read` | Mark the other participant's messages read; 200 + `UnreadCountDto` |
| POST | `/api/enquiries/{enquiryId:int}/messages` | **First message** from an enquiry; atomically creates `Conversation + Message + LastMessageAt`; 201 + `FirstMessageResultDto` |

**`MessageDto`:** `Id, ConversationId, SenderUserId, SenderName, Content, CreatedAt, ReadAt?, IsRead` (`IsRead = ReadAt != null`).
**`SendMessageRequest`:** `Content*` (max 4000).
**`FirstMessageResultDto`:** `Conversation` (`ConversationDto`) + `Message` (`MessageDto`).
**`UnreadCountDto`:** `int UnreadCount` (other participant's still-unread messages, after marking).

**Sending rules:** only the conversation's client or its managing agent may send (`message.forbidden` for Admin/others). First message additionally requires a registered client (`conversation.anonymousclient`) and an assigned agent (`conversation.noagent`). One conversation per enquiry is enforced by the unique `EnquiryId` index; concurrent first-message requests resolve server-side to exactly one conversation (concurrency retry path).

### 6.6 Agents — `api/agents`

| Method | Route | Access | Purpose |
|---|---|---|---|
| GET | `/api/agents` | Anonymous | Paginated agent list |
| GET | `/api/agents/{id:int}` | Anonymous | Agent detail |
| GET | `/api/agents/me` | Agent | Own agent profile |
| POST | `/api/agents` | Admin | Create user + agent (`IsVerified=false`) |
| PUT | `/api/agents/{id:int}` | Admin | Update (incl. `IsVerified`) |
| DELETE | `/api/agents/{id:int}` | Admin | Delete (blocked if agent owns properties) |

**`AgentDto`:** `Id, Bio, Title, Photo, Agency, LicenseNumber, Phone, Verified, FullName, UserId, Email, FirstName, LastName, CreatedAt, UpdatedAt, PropertyCount`.

### 6.7 Blog — `api/blog`

| Method | Route | Access | Purpose |
|---|---|---|---|
| GET | `/api/blog` | Anonymous | Published posts (returns `IReadOnlyList<BlogPostDto>`) |
| GET | `/api/blog/{slug}` | Anonymous | Post by slug |
| GET | `/api/blog/manage` | Admin | All posts (any status) |
| POST | `/api/blog` | Admin | Create (default `Published`) |
| PUT | `/api/blog/{id:int}` | Admin | Update |
| DELETE | `/api/blog/{id:int}` | Admin | Delete |

**`BlogPostDto`:** `Id, Title, Slug, Content, Excerpt, CoverImageUrl, Status, PublishedAt, CreatedAt, UpdatedAt, ReadMinutes` (`ReadMinutes` = content length ÷ 400, min 1).

### 6.8 Dashboard — `api/dashboard`

| Method | Route | Access | Purpose |
|---|---|---|---|
| GET | `/api/dashboard` | `[Authorize]` | Role-dispatched stats |

Dispatch: `Admin` → `AdminDashboardDto`; else `Agent` → `AgentDashboardDto`; else → `ClientDashboardDto`.

- `AdminDashboardDto(int TotalProperties, int TotalAgents, int TotalEnquiries, int TotalUsers, IReadOnlyList<PropertyDto> RecentProperties, IReadOnlyList<EnquiryDto> RecentEnquiries)` — recent = 5 each.
- `AgentDashboardDto(AgentDto Agent, int TotalProperties, IReadOnlyList<PropertyDto> RecentProperties, int TotalEnquiries, int PendingEnquiries, IReadOnlyList<EnquiryDto> RecentEnquiries)`
- `ClientDashboardDto(CurrentUserDto Profile, int TotalSavedProperties, IReadOnlyList<PropertyDto> SavedProperties, int TotalEnquiries, int PendingEnquiries, IReadOnlyList<EnquiryDto> RecentEnquiries)`

### 6.9 Saved Properties — `api/saved-properties` (`[Authorize]` class-level)

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/saved-properties` | Paginated saved `PropertyDto[]` |
| GET | `/api/saved-properties/ids` | `number[]` of saved property ids |
| POST | `/api/saved-properties/{propertyId:int}` | Save (idempotent) |
| DELETE | `/api/saved-properties/{propertyId:int}` | Unsave (idempotent) |

### 6.10 Users — `api/users` (Admin class-level)

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/users` | Paginated users (`Keyword`, `Role` filters) |

**`UserDto`:** `Id, FirstName, LastName, FullName, Email, Roles, Status ("Active"|"Suspended"), CreatedAt, AgentId?`.
`Status` = `"Suspended"` when `LockoutEnd` is in the future. When `Role` filter is given, users are filtered by role **before** pagination so counts are per-role.

### 6.11 Secured — `api/secured`

| Method | Route | Access | Purpose |
|---|---|---|---|
| GET | `/api/secured` | `[Authorize]` | `{ message = "Hello {name}, you are authenticated." }` |
| GET | `/api/secured/admin` | Admin | `{ message = "Hello Admin, you have access to this resource." }` |

---

## 7. Result / Error Pattern

### 7.1 `Result` / `Result<T>` (`src/Domain/Common/Result.cs`)

- `Result` has `IsSuccess`, `IsFailure`, `Error`; static `Success()` / `Failure(Error)`.
- `Result<T>` adds `Value` (throws if accessed on failure); static `Success(T)` / `Failure(Error)`.

### 7.2 `Error` (`src/Domain/Common/Error.cs`)

Record `Error(string Code, string Message, ErrorType Type)` with factory helpers: `NotFound`, `Validation`, `Conflict`, `Unauthorized`, `Forbidden`, `Failure`. Error codes are lowercase dotted strings, e.g. `enquiry.notfound`, `property.forbidden`, `agent.duplicateemail`.

### 7.3 HTTP mapping (`ResultExtensions.ToActionResult`)

| ErrorType | HTTP status |
|---|---|
| `NotFound` | 404 |
| `Validation` | 400 |
| `Conflict` | 409 |
| `Unauthorized` | 401 |
| `Forbidden` | 403 |
| other | 500 |

Successful `Result` → 204 NoContent; successful `Result<T>` → 200 with value. Create endpoints use `CreatedAtAction(...)` (201).

### 7.4 Error body shape

```jsonc
{ "code": "enquiry.notfound", "message": "Enquiry with id 5 was not found.", "type": "NotFound" }
```

### 7.5 Global exception handler

`GlobalExceptionHandler` (registered via `AddExceptionHandler`) returns **500** `ProblemDetails` for unhandled exceptions, logs via `ILogger`, includes `traceId`, and in Development includes message + stack trace.

### 7.6 Model validation

Invalid DataAnnotations → `400` with `{ code: "validation.requestinvalid", message: "{key}: {error}; ..." }`.

---

## 8. Data Layer

### 8.1 `AppDbContext`

`AppDbContext : IdentityDbContext<AppUser>` implements `IAppDbContext`. DbSets: `Properties`, `PropertyImages`, `Agents`, `Enquiries`, `Conversations`, `Messages`, `SaleRecords`, `LeaseRecords`, `BlogPosts`, `SavedProperties`, `AiChatSessions`, `RefreshTokens`. `IAppDbContext` exposes the same sets (read-only) + `SaveChangesAsync`. `OnModelCreating` applies configurations from the assembly.

### 8.2 Key configuration details

- **Property:** price `decimal(18,2)`; unique `Slug`; enums as strings; indexes on `(ListingType, Status)`, `City`, `State`, `Featured`, `CreatedByUserId`; Agent/CreatedByUser Restrict, images Cascade, enquiries Restrict, saved Cascade.
- **Agent:** `UserId` required (450), 1-1 Restrict; string lengths capped (Bio 4000, AgencyName 200, etc.).
- **PropertyImage:** `Url` (500), `PublicId` (200) required; index `(PropertyId, DisplayOrder)`.
- **Conversation:** `ClientUserId` required (450); **unique index on `EnquiryId`** (one conversation per enquiry); indexes on `ClientUserId` and `AgentId`; 1-1 Enquiry (Cascade), Client/Agent Restrict.
- **Message:** `SenderUserId` required (450); `Content` required (max 4000); index `(ConversationId, CreatedAt)` (covers history query + FK lookup); Conversation Cascade, Sender Restrict.
- **Enquiry:** `FullName` (200), `Email` (256), `Phone` (20), `Message` (4000); Status as string; Property Restrict, User SetNull.
- **SavedProperty:** unique index `(UserId, PropertyId)`.
- **SaleRecord/LeaseRecord:** money `decimal(18,2)`; 1-1 with Property.
- **BlogPost:** unique `Slug`; `Content` column capped at **4000** (DTO allows 100000 — posting very long content will fail at DB level).
- **AiChatSession / RefreshToken:** Cascade; refresh `Token` unique (512), indexed `UserId`.

### 8.3 Migrations (in chronological order)

| Migration | Timestamp | Adds |
|---|---|---|
| `InitialIdentity` | `20260716000151` | Identity schema (AspNetRoles/Users/Claims/Logins/UserRoles/Tokens) + `RefreshTokens` + `FirstName`/`LastName`/`CreatedAt` on users |
| `AddCoreDomainEntities` | `20260718022713` | `Agents`, `AiChatSessions`, `BlogPosts`, `Properties` (`SizeInSqM`), `Enquiries`, `LeaseRecords`, `PropertyImages`, `SaleRecords`, `SavedProperties`; refresh-token FK + unique index |
| `AlignModelWithCurrentEntities` | `20260813122123` | Rename `SizeInSqM`→`Size`; add `DisplayOrder`, `Amenities`, `Area`, `CreatedByUserId`, `Currency`, `Featured`, `Latitude`, `Longitude`, `LotSize`, `Period`, `SizeUnit`, `Slug` (unique), `YearBuilt`; agent `PhotoUrl`, `Title`; several property indexes |
| `AddAgentReadAtAndEnquiryStatusUpgrade` | `20260814120000` | `Enquiries.AgentReadAt` (timestamptz, nullable); data fix `Responded→InProgress`, `Closed→Resolved` (reversed on Down) |
| `AddConversationsAndMessages` | `20260815065905` | `Conversations` (unique `EnquiryId`, indexes `ClientUserId`/`AgentId`, 1-1 Enquiry Cascade, Client/Agent Restrict) + `Messages` (`Content` max 4000, index `(ConversationId, CreatedAt)`, Conversation Cascade, Sender Restrict); backfills `LastMessageAt` from existing messages |

Model snapshot: `ProductVersion "9.0.17"`, conventional (pluralized) table names. Migrations are **auto-applied on startup** via `dbContext.Database.MigrateAsync()`.

### 8.4 Seeders

- **`RoleSeeder`** (always): ensures roles `Admin`, `Agent`, `User` exist.
- **`DevelopmentSeeder`** (Development only; requires `SeedAdminPassword` config key, else throws). Idempotent (skips by email/slug/enquiry-email). Seeds:
  - **1 admin:** `Admin User` — `agbojisimon107@gmail.com` — role `Admin` — password from `SeedAdminPassword`.
  - **6 agents** (role `Agent`, `AgencyName="PIPDC Official"`, `IsVerified=true`, `LicenseNumber="PIPDC-XX-####"`): Nankin Bagudu, Grace Ibrahim, Daniel Dachung, Aisha Mohammed, Stephen Pam, Maryam Audu (all `@pipdc.gov.ng`).
  - **12 properties** (all Available, City `Jos`, State `Plateau`, NGN, sqm), e.g. *Highland Villa with Panoramic Plateau Views* (₦185,000,000), *Titled Land Parcel in Bukuru* (₦35,000,000), *Luxury Apartment for Lease in Rayfield* (₦4,500,000/yr); 5 featured; slugs like `highland-villa-rayfield`.
  - **4 blog posts** (Published): land titling, Rayfield neighbourhood, first-time buyer checklist, commercial real estate in Jos.
  - **4 enquiries** (no user link): 2 Pending, 1 InProgress, 1 Resolved, backdated 24–28 days.

---

## 9. Frontend Architecture

### 9.1 Provider & routing wiring (`src/App.tsx`)

```
<QueryClientProvider>          (refetchOnWindowFocus:false, retry:1, staleTime:30s)
  <AuthProvider>
    <RealtimeProvider>         (SignalR HubConnection; see 9.11)
      <ToastProvider>
        <RouterProvider router={createBrowserRouter(routes)} />
      </ToastProvider>
    </RealtimeProvider>
  </AuthProvider>
```

**Public routes** (PublicLayout): `/` Home, `/properties`, `/properties/:slug`, `/agents`, `/agents/:id`, `/blog`, `/blog/:slug`, `/about`, `/contact`.

**Auth routes** (`RedirectIfAuthenticated` → AuthLayout): `/login`, `/register`, `/forgot-password`.

**Dashboard routes** (`RequireAuth` → DashboardLayout):
- `/dashboard` → DashboardPage (role-dispatched)
- `/dashboard/properties` → StaffGuard → PropertiesSection
- `/dashboard/agents` → AdminGuard → AgentsSection
- `/dashboard/enquiries` → StaffGuard → EnquiriesSection (dispatches Admin/Agent variant)
- `/dashboard/my-enquiries` → MyEnquiriesSection
- `/dashboard/blog` → AdminGuard → BlogSection
- `/dashboard/users` → AdminGuard → UsersSection
- `/dashboard/settings` → SettingsSection
- `/dashboard/messages` → MessagingSection (Client/Agent/Admin; read-only for Admin)
- `/dashboard/saved` → SavedSection

Catch-all `*` → NotFoundPage.

### 9.2 Guards (`src/components/routing`)

| Guard | Rule | On failure |
|---|---|---|
| `RequireAuth` | authenticated | redirect `/login` (with `from` state) |
| `AdminGuard` | `roles` includes `Admin` | render `ForbiddenPage` |
| `StaffGuard` | role is `Admin` **or** `Agent` | render `ForbiddenPage` |
| `RedirectIfAuthenticated` | already logged in | redirect Admin → `/dashboard`, others → `/` |
| `RouteLoadingState` | — | full-screen spinner |

Role helpers (`src/utils/roles.ts`): `primaryRole` (Admin>Agent>Client), `isAdmin`, `isStaff`.

### 9.3 Client-side auth flow

- Axios instance `api` with `baseURL = import.meta.env.VITE_API_URL ?? '/api'`, timeout 15s.
- **localStorage keys:** `pipdc_access_token`, `pipdc_refresh_token`, `pipdc_user`.
- Request interceptor attaches `Authorization: Bearer <access>`.
- Response interceptor on 401 does a single-flight refresh (`POST /auth/refresh`), stores new tokens, retries the original request; on refresh failure clears tokens.
- `AuthContext` (`useAuth`) exposes `{ user, isAuthenticated, isRestoring, signIn, signOut, setUser }`; restores session on load via `GET /auth/me`; `signOut` revokes + clears tokens + `queryClient.clear()`.

### 9.4 Frontend service layer (`src/services/*`)

| Service | Functions (→ backend endpoint) |
|---|---|
| `authService` | login POST /auth/login; register; refresh; revoke; forgotPassword; me GET /auth/me; updateProfile PUT /auth/me; addRole; removeRole |
| `propertyService` | list GET /properties; featured; getBySlug; getById; similar; create POST; update PUT; setFeatured PUT /featured; remove DELETE |
| `enquiryService` | create POST; list GET /enquiries; getById; mine GET /enquiries/mine; agentSummaries GET /enquiries/agents/summary; byAgent GET /enquiries/agents/:id; notifyAgent POST /enquiries/:id/notify-agent; update PUT; remove DELETE |
| `savedPropertyService` | list GET /saved-properties; ids GET /saved-properties/ids; save POST /:id; unsave DELETE /:id |
| `agentService` | list; getById; me GET /agents/me; create POST; update PUT; remove DELETE |
| `blogService` | list; listManaged GET /blog/manage; getBySlug; create; update; remove |
| `dashboardService` | get GET /dashboard |
| `userService` | list GET /users |
| `conversationService` | list GET /conversations; getById GET /conversations/:id; getStateByEnquiry GET /enquiries/:enquiryId/conversation; resolveEnquiryForProperty GET /properties/:id/enquiries (find matching enquiry for a property) |
| `messageService` | list GET /conversations/:id/messages; send POST /conversations/:id/messages; sendByEnquiry POST /enquiries/:enquiryId/messages; markRead POST /conversations/:id/messages/read |

Frontend TS types in `src/types/index.ts` mirror the backend DTOs exactly (e.g. `Property`, `Enquiry`, `Agent`, `User`, `BlogPost`, `DashboardData` union, `Paginated<T>`, `AuthResponse`, `AgentEnquirySummary`, `AgentNotifyResult`).

### 9.5 TanStack Query hooks

- `queries.ts` exposes typed `useQuery` hooks per entity + structured `queryKeys` (e.g. `['properties', filters]`, `['enquiries','agents','summary']`, `['dashboard', primaryRole]`, `['conversations']`, `['conversation', id]`, `['conversationByEnquiry', enquiryId]`, `['messages', conversationId]`).
- `mutations.ts` exposes mutations that invalidate the affected query prefixes (e.g. `useCreateProperty` invalidates `['properties']`, `['agents']`, `['dashboard']`). Messaging: `useSendMessage` / `useSendFirstMessage` (optimistic append, invalidates conversation + messages), `useMarkConversationRead`.
- `useFavourites`: authenticated users → backend `saved-properties` (with optimistic cache update); anonymous users → localStorage (`pipdc_favourites`).
- Real-time hooks: `useConversationSubscription(conversationId)` (joins/leaves the SignalR group) and `useNewMessageListener()` (folds incoming `NewMessage` events into the `['messages', id]` cache).

### 9.6 Dashboards

- **`DashboardPage`** fetches `GET /dashboard` and dispatches by role to `AdminDashboard`, `AgentDashboard`, or `ClientDashboard`; warns if the payload doesn't match the role.
- **`AdminDashboard`:** 4 stat cards (Properties, Agents, Enquiries, Users) + recent Properties list + recent Enquiries list.
- **`AgentDashboard`:** profile card + 3 stats (My Properties, My Enquiries, Pending Enquiries) + recent properties/enquiries.
- **`ClientDashboard`:** profile + 3 stats (Saved, My Enquiries, Pending) + saved properties + recent enquiries.
- **Sections** (`sections/*`): `PropertiesSection` (admin all / agent own; add/edit via `PropertyForm`; featured toggle; delete), `EnquiriesSection` (dispatcher), `AgentEnquiriesSection` (table, inline status `<select>`, unread gold tint + "New" badge, eye action re-fetches to mark read), `AdminEnquiriesSection` (accordion per agent from summaries; Bell → notify-agent; toast "Email delivery is not enabled yet"), `MyEnquiriesSection` (read-only), `SavedSection`, `UsersSection` (promote/demote Agent with property-transfer notice), `AgentsSection` (create via `AgentForm`, verify toggle, delete with listing guard), `BlogSection` (manage via `BlogForm`), `SettingsSection` (profile update + account summary).

### 9.7 Forms (`react-hook-form` + `zodResolver`)

- **`PropertyForm`** — full property fields; amenities/images as newline-separated text; admin-only agent assignment; featured forced false for non-admins.
- **`AgentForm`** — create requires email/password; edit shows "Verified agent" checkbox.
- **`BlogForm`** — title/slug/excerpt/cover/status/content; new posts default Draft.

### 9.8 Public pages

- **Home** — hero (with search filter), featured properties, why-choose, agents, stats (animated `useCountUp`), latest properties, insights, CTA.
- **`PropertiesPage`** — filter sidebar (SearchFilter), grid/list toggle, favourites toggle, pagination (9 per page).
- **`PropertyDetailsPage`** — gallery, specs, amenities, **enquiry form** (creates a real enquiry; requires login — anonymous visitors are redirected to `/login?from=...`), save/share, agent sidebar (call/email), "Message agent" → `/dashboard/messages?enquiry={id}` when the visitor already enquired, similar properties.
- **`AgentsPage` / `AgentProfilePage`** — agent grid, client-side search, per-agent listings.
- **Blog, About, Contact** — blog cards/detail, about timeline, contact form (simulated submit — no backend call).
- **Auth pages** — Login (remember-me, redirects Admin to `/dashboard`), Register (terms checkbox → `/login`), ForgotPassword (success panel).
- **Error pages** — 404 and 403 (Forbidden).

### 9.9 Layouts & UI kit

`PublicLayout`, `AuthLayout` (split panel), `DashboardLayout` (sidebar + header), `Navbar` (role-aware), `Sidebar` (role-based nav groups: Admin/Agent/Client), `Footer`. UI primitives: `Button`, `Badge`, `Card`, `Input`/`Select`/`Textarea`, `Modal`, `Toast`, `Spinner`, `Pagination`, `EmptyState`, `ConfirmDialog`, `Breadcrumb`, `SectionHeading`. Domain components: `PropertyCard`, `PropertyCardSkeleton`, `SearchFilter`, `AgentCard`, `Logo`, `StatCard`, `ProfileCard`, `EnquiryList`, `PropertyList`.

### 9.10 Mock data & known placeholders

- The **only** mock data is `plateauLocations` (10 Plateau locations) used to populate the location filter and "Popular locations" list. **All other frontend data comes from the real API.**
- Placeholders: ContactPage submit is simulated; footer newsletter does nothing; dashboard search/notifications buttons are decorative; property-details map is a static placeholder.

### 9.11 Messaging & real-time (Phase 2)

**Components (`src/components/messaging`):**
- `MessagingSection` — conversation list + active thread two-pane view; reads `?enquiry=` query param to auto-open/continue a thread; hosts the real-time hooks.
- `ConversationList` / `ConversationItem` — paginated list, unread badge, relative `LastMessageAt`.
- `ConversationView` — active thread header (client/agent/property info) + `MessageList` + `MessageComposer`.
- `MessageList` / `MessageBubble` — own vs. other bubbles, sender name, timestamps, read ticks, auto-scroll; stop-propagation so mark-read polling never re-fires on every render.
- `MessageComposer` — textarea + send; disables while sending.
- `NewConversationView` — client-side flow: pick one of the caller's own enquiries for a property, then send the **first message** (→ `POST /enquiries/{id}/messages`).

**Real-time client (`@microsoft/signalr` ^10.0.11):**
- `RealtimeContext.tsx` builds a single shared `HubConnection` at `HUB_URL = baseURL.replace(/\/api\/?$/, '') + '/hubs/messaging'` with `accessTokenFactory: () => tokenStore.getAccess() ?? ''` and `.withAutomaticReconnect()`. It starts on `signIn`/mount when authenticated and stops on `signOut`; exposure `useRealtime()`.
- `useConversationSubscription(conversationId)` calls `JoinConversation(id)` / `LeaveConversation(id)` whenever the selected conversation changes and re-establishes membership after a (re)connect.
- `useNewMessageListener()` registers for server event `'NewMessage'`, validates the `Message` payload, and **folds it into the `['messages', conversationId]` cache only if that query is already fetched** (deduped by `Message.Id`) — otherwise the next fetch gets it. This keeps the DB as source of truth.
- Sending still goes through REST; the UI renders the optimistic value and reconciles against the server payload.

---

## 10. Key Business Workflows

### 10.1 Client journey

1. Browse `/properties` (filter + search) or home.
2. Open a property → view details; optionally **save** (favourite) or submit an **enquiry** (login required; identity taken from the account — no name/email/phone form fields).
3. Register → role `User`.
4. Dashboard shows saved properties, my enquiries, pending count.
5. Enquiry appears to the assigned **agent** as unread (`IsRead=false`).
6. After an agent replies, the client's conversation thread in `/dashboard/messages` updates in real time (SignalR `NewMessage`).

### 10.2 Agent journey

1. Log in with role `Agent` → dashboard: my properties, my enquiries, pending count.
2. `/dashboard/enquiries` lists only enquiries for the agent's own properties.
3. Opening an enquiry (eye action → `GET /enquiries/{id}`) **marks it read** (`AgentReadAt` set).
4. Update enquiry status inline (`Pending → InProgress → ViewingScheduled → Resolved`).
5. Reply from the enquiry's **Message** action → first message atomically creates the conversation; subsequent replies go through the thread in `/dashboard/messages`, delivered live to the client via SignalR.

### 10.3 Admin journey

1. Admin dashboard: platform-wide stats.
2. `/dashboard/agents` — create agents (auto-creates user+agent, `IsVerified=false`), verify, delete (blocked if listings).
3. `/dashboard/users` — promote/demote users to/from `Agent`; demotion transfers properties to the admin agent.
4. `/dashboard/enquiries` — per-agent accordion (summaries + drill-down), **Notify Agent** (payload only, email not yet implemented).
5. `/dashboard/properties` — manage all properties; assign to any agent.
6. `/dashboard/blog`, `/dashboard/settings`.

---

## 11. Development Roadmap — Current Position

The project follows `PIPDC_DEVELOPMENT_ROADMAP.md` (the source of truth; 15 sequential phases).

| Phase | Status |
|---|---|
| **1. Enquiry Foundation** | **COMPLETED** — status lifecycle (Pending/InProgress/ViewingScheduled/Resolved), agent read/unread tracking, property enquiry counts, agent grouping, admin visibility, notify-agent foundation, agent-scoped access, property→enquiry relationship |
| **2. Messaging / Conversations** | **COMPLETED** — Conversation + Message entities, atomic first-message creation (one conversation per enquiry), client↔agent messaging, read/unread, last-message, admin view-only; SignalR `/hubs/messaging` (JWT auth via `sub`, per-conversation groups, `NewMessage` broadcast after persistence, best-effort delivery) + frontend real-time client |
| 3. SMTP / Email notifications | **NEXT** (current phase) — Google SMTP via `IEmailService` abstraction; credentials in user secrets |
| 4. Property Development model | Later (DevelopmentProject/Unit/Update/Tracking) |
| 5. Users / Agent management | Later (incl. property-ownership rule on Agent-role removal) |
| 6. Property management | Later |
| 7. Saved properties / favourites | Later (client UX) |
| 8. Blog / content | Later |
| 9. Locations | Later (State→LGA→City→Area) |
| 10. Dashboard refinement | Later |
| 11. Full system integration testing | Later |
| 12. Production hardening | Later (concurrency, idempotency, rate limiting, caching, DB opt, background processing, observability, security) |
| 13. CI/CD + deployment | Later |
| 14. AI recommendation system | Later (provider abstraction, e.g. Gemini/Groq/OpenRouter) |
| 15. AI expansion | Later |

Core rules: strictly follow phase order; backend-first when the domain changes; no premature infrastructure; frontend must use real API contracts; the developer (not the assistant) runs the servers and tests.

---

## 12. Configuration & Secrets

| Item | Where | Notes |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | user secrets (dev) / env (prod) | PostgreSQL connection; empty in checked-in `appsettings.json` |
| `JwtSettings:Key` | user secrets (dev) / env (prod) | Signing key — **never committed** |
| `JwtSettings:Issuer` | appsettings | `https://pipdc.plateaustate.gov.ng` |
| `JwtSettings:Audience` | appsettings | `pipdc-api` |
| `Cors:AllowedOrigins` | appsettings | dev allows `http://localhost:5173` |
| `SeedAdminPassword` | user secrets (dev only) | admin seed password; seeder throws without it |
| `VITE_API_URL` (frontend `.env`) | frontend env | `https://localhost:7123/api` in dev |

> **Security rule:** credentials (JWT key, DB password, future SMTP credentials) must live in user secrets during development and environment variables/secrets in production. They must never be committed.

---

## 13. Development Conventions

- Controllers are thin; services return `Result<T>`; no expected failures throw.
- `[Authorize(Roles = "…")]` on endpoints; role checks use `"Admin"`/`"Agent"` strings via `Roles.*`.
- Current-user id read via `User.FindFirstValue(JwtRegisteredClaimNames.Sub)`; roles via `User.FindAll("role")`.
- Error codes are lowercase dotted (`domain.code`); messages are human-readable sentences.
- Pagination defaults: page 1, size 10 (properties/agents 100 max, enquiries 100 max, users 50 max).
- Computed `AppUser.FullName` is safe in EF `Select` projections but must never appear in a SQL `GROUP BY` (resolve names in memory instead).
- Seed data is idempotent (keyed by email/slug); seeding only runs in Development.
- Frontend: TanStack Query for server state (typed queryKeys + invalidation), zod-validated forms, axios with automatic token refresh, role-aware routing guards.
- **SignalR conventions:** hub path `/hubs/messaging`; JWT via `access_token` query param (hub path only); user identity = JWT `sub` (`JwtSubUserIdProvider`); groups named `conversation:{conversationId}` (`ConversationGroup`); the single client event is `NewMessage`; group membership (`JoinConversation`/`LeaveConversation`) is authorized server-side with `ConversationAuthorization`; the DB is the source of truth and broadcasts are best-effort after successful persistence (never fail the REST write); `IHubContext<MessagingHub>` is injected into services, the Hub never broadcasts.
- **Messaging rules:** one conversation per enquiry (unique `EnquiryId`, enforced at DB level); a conversation is created only by a successful first message (atomic `Conversation + Message + LastMessageAt`); only the conversation's client or managing agent may send; Admin is view-only; the frontend's conversation-state endpoint is read-only and never creates data.
