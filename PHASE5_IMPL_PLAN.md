# Phase 5 — Users / Agent Management: Implementation Plan

## Overview

Complete the user and agent management workflows already partially built. The `Agent` entity already has `Bio`, `LicenseNumber`, and `IsVerified` fields. The `RemoveRoleAsync` already handles property transfer. This phase fills the remaining gaps: user profile viewing, soft-deactivation, agent verification toggle, and agent summary endpoint.

**No entity changes. No migration needed. No DI registration changes.**

---

## Step 1 — UserService: GetUserById

Add `GetByIdAsync` to view a single user's full profile (admin operation).

### Modify `src/Application/Users/IUserService.cs`
Add method:
```
GetByIdAsync(userId, ct) → Result<UserDetailDto>
```

### Modify `src/Application/Users/Dtos.cs`
Add new DTO:
```
UserDetailDto(
    string Id, string FirstName, string LastName, string FullName,
    string Email, string? PhoneNumber, DateTime CreatedAt,
    IEnumerable<string> Roles, string Status,
    int? AgentId, string? AgentLicenseNumber, string? AgentAgencyName, bool? AgentIsVerified)
```

Keep existing `UserDto` unchanged (used by list endpoint).

### Modify `src/Application/Users/UserService.cs`
- `GetByIdAsync`: Look up user by ID via `userManager.FindByIdAsync`
- Include roles via `userManager.GetRolesAsync`
- Include agent info via `dbContext.Agents.FirstOrDefaultAsync(a => UserId == userId)`
- Compute status from `LockoutEnd`
- Return `UserDetailDto` or `Error.NotFound`

---

## Step 2 — UserService: Deactivate + Activate

Soft-deactivate users using Identity's built-in `LockoutEnd` mechanism. No new entity fields needed.

### Modify `src/Application/Users/IUserService.cs`
Add methods:
```
DeactivateAsync(userId, ct) → Result
ActivateAsync(userId, ct) → Result
```

### Modify `src/Application/Users/UserService.cs`
- `DeactivateAsync`:
  - Find user by ID
  - Prevent self-deactivation (`Error.Conflict`)
  - Prevent deactivating the last admin (`Error.Conflict`)
  - Set `LockoutEnd = DateTimeOffset.MaxValue`
  - Save changes
- `ActivateAsync`:
  - Find user by ID
  - Set `LockoutEnd = null`
  - Save changes

---

## Step 3 — UsersController: New endpoints

### Modify `src/API/Controllers/UsersController.cs`
Add 3 endpoints:

| Method | Route | Calls | Description |
|---|---|---|---|
| `GET` | `/api/users/{id}` | `GetByIdAsync` | View single user profile |
| `POST` | `/api/users/{id}/deactivate` | `DeactivateAsync` | Soft-deactivate user |
| `POST` | `/api/users/{id}/activate` | `ActivateAsync` | Reactivate user |

All require `[Authorize(Roles = Roles.Admin)]` (inherited from class).

---

## Step 4 — AgentService: ToggleVerification

Dedicated endpoint to flip agent verification status.

### Modify `src/Application/Agents/IAgentService.cs`
Add method:
```
ToggleVerificationAsync(agentId, ct) → Result<AgentDto>
```

### Modify `src/Application/Agents/AgentService.cs`
- Find agent by ID (include User)
- Flip `IsVerified = !IsVerified`
- Set `UpdatedAt = DateTime.UtcNow`
- Save changes
- Return updated DTO with property count

---

## Step 5 — AgentService: GetSummary

Full agent profile with related entity counts.

### Modify `src/Application/Agents/IAgentService.cs`
Add method:
```
GetSummaryAsync(agentId, ct) → Result<AgentSummaryDto>
```

### Modify `src/Application/Agents/Dtos.cs`
Add new DTO:
```
AgentSummaryDto(
    int Id, string? Bio, string? Title, string? Photo, string Agency,
    string? LicenseNumber, string Phone, bool Verified,
    string FullName, string UserId, string Email,
    DateTime CreatedAt, DateTime? UpdatedAt,
    int PropertyCount, int EnquiryCount, int ConversationCount)
```

### Modify `src/Application/Agents/AgentService.cs`
- Load agent with User
- Count `Properties.Where(AgentId == id)`
- Count `Enquiries` via join on property ownership
- Count `Conversations` where agent participates
- Return `AgentSummaryDto`

---

## Step 6 — AgentsController: New endpoints

### Modify `src/API/Controllers/AgentsController.cs`
Add 2 endpoints:

| Method | Route | Calls | Description |
|---|---|---|---|
| `PUT` | `/api/agents/{id}/verify` | `ToggleVerificationAsync` | Toggle verification |
| `GET` | `/api/agents/{id}/summary` | `GetSummaryAsync` | Full profile + stats |

---

## Step 7 — Build & Verify

- `dotnet build PIPDC.csproj` — 0 errors

---

## Files Summary

| Action | File | Change |
|---|---|---|
| Modify | `src/Application/Users/IUserService.cs` | Add 3 methods |
| Modify | `src/Application/Users/Dtos.cs` | Add `UserDetailDto` |
| Modify | `src/Application/Users/UserService.cs` | Implement 3 methods |
| Modify | `src/API/Controllers/UsersController.cs` | Add 3 endpoints |
| Modify | `src/Application/Agents/IAgentService.cs` | Add 2 methods |
| Modify | `src/Application/Agents/Dtos.cs` | Add `AgentSummaryDto` |
| Modify | `src/Application/Agents/AgentService.cs` | Implement 2 methods |
| Modify | `src/API/Controllers/AgentsController.cs` | Add 2 endpoints |
| **Total** | **8 files** | All modifications, 0 new files |
