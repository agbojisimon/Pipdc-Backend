# PIPDC — FINAL DEVELOPMENT ROADMAP

> **IMPORTANT:** This roadmap is the FINAL development roadmap for the current PIPDC development cycle.
>
> The purpose of this document is to prevent scope creep, prevent us from jumping between unrelated features, and ensure that development is completed in the exact order defined below.
>
> - DO NOT modify the order.
> - DO NOT add new major phases.
> - DO NOT remove phases.
> - DO NOT redesign the roadmap.
> - DO NOT introduce alternative architectures into this roadmap.
>
> If a new idea or feature is discovered during development, it must be treated as a future enhancement unless it is clearly required to complete the current phase.

---

## PROJECT GOAL

Complete the core PIPDC real-estate platform within the current development cycle, then stabilize and harden it for production, followed by the AI recommendation system.

Development must proceed sequentially.

The priority is:

1. Functional completeness
2. System integration
3. Production hardening
4. Deployment
5. AI integration

Do not prematurely optimize or introduce infrastructure complexity before the core domain functionality is complete.

---

## PHASE 1 — ENQUIRY FOUNDATION

### STATUS: COMPLETED

**Backend:**

- Enquiry status lifecycle:
  - Pending
  - InProgress
  - ViewingScheduled
  - Resolved
- Agent read/unread tracking
- Property enquiry counts
- Agent enquiry grouping
- Admin enquiry visibility
- Admin "Notify Agent" foundation
- Agent-specific enquiry access
- Property → Enquiry relationship

**Frontend:**

- Agent enquiry management
- Client enquiry tracking
- Admin → Agent → Enquiries hierarchy
- Unread indicators
- Property navigation from enquiry

This phase is COMPLETE.

---

## PHASE 2 — MESSAGING / CONVERSATIONS

### STATUS: COMPLETED

Build the complete client ↔ agent communication system.

**Backend:**

Create the messaging domain:

```
Conversation
Message
```

Relationship:

```
Client
   ↓
Conversation
   ↓
Message
   ↓
Agent
```

And:

```
Conversation
   ↓
Enquiry
   ↓
Property
```

Implement:

- [x] Conversation entity
- [x] Message entity
- [x] Conversation → Enquiry relationship
- [x] Message → Sender relationship
- [x] Message authorization
- [x] Client ↔ Agent messaging
- [x] Agent ↔ Client replies
- [x] Admin visibility
- [x] Message history
- [x] Read/unread tracking
- [x] Last-message tracking
- [x] Conversation ownership/security
- [x] Property-specific conversation context

Implemented during this phase:

- [x] Authenticated-only enquiry creation
- [x] Enquiry identity automatically derived from authenticated user
- [x] Client/agent messaging integration from enquiry

**Frontend:**

- [x] Conversation list
- [x] Chat interface
- [x] Message history
- [x] Send message
- [x] Reply functionality
- [x] Read/unread indicators
- [x] "View Property"
- [x] Client conversation interface
- [x] Agent conversation interface
- [x] Admin conversation visibility

Implemented during this phase:

- [x] Mobile-specific conversation list → conversation detail navigation
- [x] Prevention of frontend mark-read request loop

**REAL-TIME:**

### STATUS: COMPLETED

SignalR for real-time message delivery is implemented end-to-end:

- [x] Hub endpoint `/hubs/messaging` (`MessagingHub`, `[Authorize]`)
- [x] JWT authentication for SignalR (`access_token` query-parameter extraction for the hub path only; token validation unchanged)
- [x] SignalR user identity from the JWT `sub` claim (`JwtSubUserIdProvider`)
- [x] Conversation group naming convention `conversation:{conversationId}` (`ConversationGroup`)
- [x] `JoinConversation(int)` / `LeaveConversation(int)` with server-side authorization (reuses `ConversationAuthorization`; client never supplies identity, roles, or group names)
- [x] `NewMessage` event broadcast to `conversation:{id}` only after successful database persistence
- [x] Broadcast is best-effort: a SignalR delivery failure is logged and does not fail the already-persisted REST operation
- [x] Frontend SignalR client (`RealtimeProvider`, `useConversationSubscription`, `useNewMessageListener`)
- [x] Multiple connections per user handled via SignalR group membership (no manual connection tracking)

Important architectural rule:

The database remains the source of truth.

SignalR is the real-time delivery mechanism, not the persistence mechanism.

### CURRENT STEP — ATOMIC FIRST-MESSAGE CONVERSATION CREATION

STATUS: COMPLETED

- [x] Opening the messaging UI does NOT create a Conversation (read-only state endpoint)
- [x] A Conversation is created only when the first message is successfully sent
- [x] First-message creation atomically creates Conversation + Message + LastMessageAt
- [x] If the operation fails, the entire operation rolls back
- [x] Existing Conversation.EnquiryId UNIQUE constraint retained
- [x] Concurrent first-message requests resolve to exactly one Conversation (DbUpdateException retry path)
- [x] SignalR broadcast fires exactly once, only after the transaction commits

This phase is COMPLETE.

---

## PHASE 3 — EMAIL NOTIFICATIONS

### STATUS: COMPLETED

Implement email notifications after messaging is stable.

Provider:

Gmail API (OAuth2) via Google.Apis.Gmail.v1

Abstraction:

```
IEmailService
    ↓
GmailApiEmailService
    ↓
Gmail API (OAuth2)
```

Email events:

- New enquiry → Agent
- Agent reply → Client
- Client reply → Agent
- Viewing scheduled → Client + Agent
- Enquiry resolved → Client
- Admin manually notifies Agent

Emails contain appropriate CTA buttons such as:

"View & Reply to Enquiry"

The CTA takes the recipient directly to the appropriate PIPDC page/conversation.

**Security:**

- Gmail OAuth2 credentials stored in User Secrets only
- Never committed to Git
- Never hardcoded

Emails are sent synchronously.

Background email processing can be introduced later during production hardening.

This phase is COMPLETE.

---

## PHASE 4 — PROPERTY DEVELOPMENT MODEL

### STATUS: COMPLETED

Create a proper domain model for properties/projects under development.

Do NOT overload the normal PropertyStatus enum to represent development projects.

Core entities:

```
DevelopmentProject
DevelopmentUnit
DevelopmentUpdate
DevelopmentTracking
```

Relationship:

```
DevelopmentProject
    ├── DevelopmentUnit
    └── DevelopmentUpdate
```

```
Client
    ↓
DevelopmentTracking
    ↓
DevelopmentProject / DevelopmentUnit
```

DevelopmentProject should support concepts such as:

- Name
- Description
- Location
- Developer
- Status
- Expected completion date
- Images
- Progress percentage
- Units

DevelopmentUnit:

- Unit identifier
- Unit type
- Status
- Price/details as required
- Relationship to DevelopmentProject

DevelopmentUpdate:

- Title
- Description
- Progress
- Date
- Images

DevelopmentTracking:

- Client
- Project
- Optional Unit
- Tracking state
- Dates

**Frontend:**

Client:

- Projects I'm Tracking
- Development details
- Units
- Progress
- Development updates
- Images
- Stop tracking

Admin:

- Create development
- Edit development
- Manage units
- Add development updates
- Manage images
- Manage tracking

---

## PHASE 5 — USERS / AGENT MANAGEMENT

### STATUS: COMPLETED

Complete the user and agent management workflows.

**Admin:**

- Create user
- View users
- Search users
- Filter users
- View user profile
- Assign Agent role
- Remove Agent role
- Deactivate/delete user
- Verify agent
- Manage agent license information

**Agent:**

- Agent profile
- License information
- Verification status
- Properties handled
- Enquiries
- Clients/enquiries associated with properties

**IMPORTANT BUSINESS RULE:**

Before implementing Agent role removal, formally implement the property ownership rule.

Preferred direction:

Removing an Agent role MUST NOT delete their properties.

Properties should instead be transferred to Admin/unassigned or explicitly reassigned according to the final business rule.

---

## PHASE 6 — PROPERTY MANAGEMENT

### STATUS: COMPLETE

Complete the property management workflow.

**DONE:**

- [x] Create property
- [x] Read property (list, detail, slug, similar)
- [x] Update property
- [x] Delete property
- [x] Set featured (Admin toggle)
- [x] Enquiry count (displayed in dashboard table)
- [x] Property form with Zod validation
- [x] Agent assignment on create (Admin dropdown, Agent self-assign)
- [x] Development projects separated from property lifecycle
- [x] Fix PropertyStatus enum — Available/Pending/Sold/Rented/Unavailable
- [x] Image upload — Cloudinary integration + frontend upload widget with preview
- [x] Image remove — dedicated delete endpoint + UI
- [x] Dedicated status change endpoint — PATCH /api/properties/{id}/status
- [x] Dedicated listing type change endpoint — PATCH /api/properties/{id}/listing-type
- [x] Agent assign/reassign endpoint — PUT /api/properties/{id}/agent (Admin only)
- [x] Agent unassign — nullable AgentId FK, Admin can set null
- [x] View property enquiries per property — GET /api/enquiries/property/{propertyId}
- [x] Status/listing-type quick-update UI — inline dropdown in PropertiesSection
- [x] Agent reassignment UI — inline dropdown in PropertiesSection (Admin only)
- [x] Per-property enquiry view — PropertyDetailsPage section (Admin/Agent)
- [x] Fix status/listing-type coupling — TryResolveListing now preserves existing status when only listing type is sent

Normal property lifecycle (target):

- Available
- Pending
- Sold
- Rented
- Unavailable

Development projects have their own lifecycle and MUST NOT be mixed into the normal property lifecycle.

---

## PHASE 7 — SAVED PROPERTIES / FAVOURITES

### STATUS: COMPLETE

Complete the client UX.

**DONE:**

- [x] Save property (endpoint + optimistic UI + guest localStorage fallback)
- [x] Unsave property (endpoint + heart toggle everywhere)
- [x] View saved properties (paginated list in dashboard)
- [x] View saved properties in dashboard (stat card + top-5 on home + dedicated /dashboard/saved section)
- [x] Quickly open property (View link in saved rows)
- [x] Heart hydration from saved IDs endpoint
- [x] IsSaved flag embedded in PropertyDto
- [x] Quickly enquire from saved properties — Enquire row action navigates to messages
- [x] Fix "Saved at" timestamp — SavedPropertyDto surfaces SavedAt from backend, SavedSection displays correct timestamp
- [x] Fix saved-ordering bug — properties now returned in saved-date order, not property creation date

Desired user journey:

```
Browse Property
    ↓
Save Property
    ↓
Saved Properties
    ↓
Enquiry
    ↓
Conversation
```

---

## PHASE 8 — BLOG / CONTENT

### STATUS: IN PROGRESS (~70%)

**DONE:**

- [x] Blog listing (public, paginated, keyword search)
- [x] Blog details (by slug)
- [x] Admin create post
- [x] Edit post
- [x] Delete post
- [x] Publish/unpublish (via Status field in form)
- [x] Admin management table with status badges
- [x] Cover image URL support
- [x] Auto-generated slug (unique, indexed)
- [x] Excerpt support

**REMAINING:**

- [ ] Featured posts — add IsFeatured flag to entity + DTOs, sort/filter support, home page featured section (currently just shows latest 3)
- [ ] SEO metadata — add MetaTitle, MetaDescription fields to entity/DTOs; implement per-page head management on frontend (react-helmet-async or equivalent); add OG/Twitter tags
- [ ] Publish/unpublish ergonomics — dedicated PUT /api/blog/{id}/publish|unpublish endpoint that clears PublishedAt on unpublish; add one-click toggle in BlogSection admin table
- [ ] Fix draft-exposure bug — GetBySlugAsync returns any post regardless of status; must filter to Published for anonymous users (admin bypass)
- [ ] Fix content length mismatch — DTOs allow MaxLength(100000) but EF config caps Content at 4000 chars
- [ ] Image upload — currently URL paste only; need upload endpoint + widget with preview (or remove dead CoverImagePublicId column)
- [ ] Categories (optional per roadmap) — decide if required; if so, add entity + DTOs + filtering
- [ ] Tags (optional per roadmap) — decide if required; if so, add entity + DTOs + filtering

---

## PHASE 9 — LOCATIONS

### STATUS: NOT STARTED

Replace the current frontend-only location approach when appropriate.

Potential hierarchy:

```
State
    ↓
LGA
    ↓
City
    ↓
Area
```

Locations will support:

- Property filtering
- Property search
- Development projects
- Future AI recommendations

Do NOT over-engineer this phase.

---

## PHASE 10 — DASHBOARD REFINEMENT

### STATUS: NOT STARTED

Only after the core functionality is working should the dashboards receive major UI refinement.

**ADMIN:**

- Properties
- Agents
- Users
- Enquiries
- Conversations
- Developments
- Recent activity
- Company-level statistics

**AGENT:**

- My properties
- Enquiries
- Enquiry counts per property
- Conversations
- Clients
- Profile

**CLIENT:**

- Saved properties
- Enquiries
- Conversations
- Tracked development projects
- Profile

Focus first on correctness and usability.

Visual polish comes after functionality.

---

## PHASE 11 — FULL SYSTEM INTEGRATION TESTING

### STATUS: NOT STARTED

Test complete business workflows rather than only isolated endpoints.

Example client journey:

```
Register
    ↓
Browse property
    ↓
Save property
    ↓
Submit enquiry
    ↓
Agent receives enquiry
    ↓
Agent opens enquiry
    ↓
Agent replies
    ↓
Client receives reply
    ↓
Client replies
    ↓
Viewing scheduled
    ↓
Viewing completed
    ↓
Enquiry resolved
```

Example admin/agent journey:

```
Admin
    ↓
Creates/assigns Agent
    ↓
Assigns Property
    ↓
Client enquires
    ↓
Agent receives enquiry
    ↓
Agent responds
    ↓
Admin monitors activity
```

Validate:

- Authorization
- Ownership
- Role restrictions
- Data consistency
- Error handling
- Frontend/backend contracts
- Real-world workflows

---

## PHASE 12 — PRODUCTION HARDENING

### STATUS: NOT STARTED

DO NOT implement these major system-design optimizations prematurely.

They come after the business functionality is stable.

### 12.1 CONCURRENCY CONTROL

Protect operations such as:

- Property assignment
- Agent assignment
- Enquiry status changes
- Viewing scheduling
- Development updates
- User role changes

Prevent conflicting updates from silently overwriting each other.

### 12.2 IDEMPOTENCY

Apply where repeated requests could create duplicate side effects.

Examples:

- Enquiry creation
- Notifications
- Email sending
- Viewing scheduling
- Future payment operations

### 12.3 RATE LIMITING

Protect public/high-risk endpoints:

- Login
- Register
- Forgot password
- Enquiry creation
- Messaging
- AI endpoints

### 12.4 CACHING

Introduce caching only where actual access patterns justify it.

Potential candidates:

- Featured properties
- Popular properties
- Locations
- Blog posts
- Search results where appropriate
- AI recommendations

Redis may be introduced when justified.

### 12.5 DATABASE OPTIMIZATION

Review:

- Database indexes
- Query performance
- EF Core projections
- N+1 queries
- Pagination
- Search queries
- Slow joins
- Query plans

### 12.6 BACKGROUND PROCESSING

Move expensive/non-critical operations away from HTTP requests where appropriate.

Potential workloads:

- Email
- Notifications
- AI processing
- Image processing
- Analytics

### 12.7 OBSERVABILITY

Introduce:

- Structured logging
- Correlation IDs
- Error tracking
- Health checks
- Metrics
- Audit logs

### 12.8 SECURITY HARDENING

Review:

- JWT handling
- Refresh tokens
- Authorization
- Rate limiting
- Input validation
- File uploads
- CORS
- Secrets management
- Security headers
- OWASP-related risks

---

## PHASE 13 — CI/CD + DEPLOYMENT

### STATUS: NOT STARTED

Establish a proper deployment workflow.

Pipeline:

```
Git
 ↓
CI
 ↓
Build
 ↓
Tests
 ↓
Migration strategy
 ↓
Deployment
```

Environment separation:

```
Development
    ↓
Staging
    ↓
Production
```

Ensure secrets and environment-specific configuration are handled correctly.

---

## PHASE 14 — AI PROPERTY RECOMMENDATION SYSTEM

### STATUS: NOT STARTED

Only begin AI after the domain model is stable.

DO NOT train a custom model initially.

Build:

```
PIPDC Data
    ↓
Recommendation Logic
    ↓
AI Provider
```

Use an abstraction:

```
IAIRecommendationService
        ↓
AIRecommendationService
        ↓
AI Provider
```

The provider must be replaceable.

Potential initial providers to research when this phase begins:

- Google Gemini
- Groq-hosted open models
- OpenRouter
- Hugging Face inference
- Other suitable free/open-model providers

Provider selection MUST be researched at the time of implementation because:

- Free tiers change
- Models change
- Rate limits change
- Commercial-use policies change

Initial AI recommendation inputs may include:

- Location
- Budget
- Property type
- Bedrooms
- Listing type
- Client preferences
- Previous searches
- Saved properties
- Enquiries
- Development interests

Recommendations should provide useful explanations where appropriate.

Example:

"Recommended because you previously saved 3-bedroom properties around Rayfield and your stated budget is ₦80m."

---

## PHASE 15 — AI EXPANSION

### STATUS: NOT STARTED

After recommendations are working:

- Natural-language property search
- Property assistant
- Client preference profiling
- Personalized recommendations
- Property discovery
- Admin/Agent intelligence

Potential queries:

"Find me a 4-bedroom house around Rayfield under ₦100m."

"Which properties received the most enquiries this month?"

"Which agents have unresolved enquiries?"

The AI must solve real PIPDC problems rather than simply becoming a generic chatbot.

---

## FINAL DEVELOPMENT RULES

1. **FOLLOW THE PHASE ORDER.**

   Do not jump to a later phase because it looks interesting.

2. **DO NOT INTRODUCE PREMATURE OPTIMIZATION.**

   Do not add Redis, queues, complex concurrency mechanisms, or other infrastructure simply because they are technically interesting.

3. **DO NOT CHANGE THE ROADMAP DURING THIS DEVELOPMENT CYCLE.**

   If a new feature is proposed, classify it as:
   - Required for current phase
   - Required for a later phase
   - Future enhancement

4. **PRESERVE THE DOMAIN MODEL.**

   Do not hack new business concepts into existing entities when a proper domain entity is more appropriate.

5. **BACKEND FIRST WHEN THE DOMAIN CHANGES.**

   Establish the backend contract/domain model first, then integrate the frontend.

6. **FRONTEND MUST USE REAL API CONTRACTS.**

   Do not fabricate data simply to make UI features appear functional.

7. **TESTING SHOULD BE PERFORMED BY THE DEVELOPER.**

   OpenCode should NOT automatically start all project servers, restart the API, run lengthy live smoke tests, or perform unnecessary end-to-end testing unless explicitly instructed.

   OpenCode's primary responsibility during implementation is to make the requested code changes.

   The developer will manually run/build/test the application when appropriate.

8. **DO NOT STOP DEVELOPMENT BECAUSE A LATER FEATURE IS NOT IMPLEMENTED.**

   Implement only what belongs to the current phase.

9. **DOCUMENT IMPORTANT ARCHITECTURAL DECISIONS.**

   When a significant architectural decision is made, document it separately rather than silently changing the roadmap.

10. **THE ROADMAP IS THE SOURCE OF TRUTH.**

    At the beginning of each major task, identify the current phase.

    Do not proceed to the next phase until the current phase is functionally complete.

---

## CURRENT POSITION

Completed:
**PHASE 1 — Enquiry Foundation**
**PHASE 2 — Messaging / Conversations**
**PHASE 3 — Email Notifications**
**PHASE 4 — Property Development Model**
**PHASE 5 — Users / Agent Management**
**PHASE 6 — Property Management**

Current:
**PHASE 7 — Saved Properties / Favourites** (~90% — two small gaps remaining)
**PHASE 8 — Blog / Content** (~70% — featured posts, SEO, and draft-exposure fix needed)

Next after Phase 8:
**PHASE 9 — Locations**

Continue sequentially through the remaining roadmap phases.

---

## SUCCESS CRITERIA

The current development cycle is considered successful when:

1. Core PIPDC business workflows are functional.
2. Client, Agent, and Admin experiences are correctly separated.
3. Messaging and notifications work.
4. Property development tracking works.
5. Property, user, agent, enquiry, saved-property, blog and development workflows are integrated.
6. Full system integration testing passes.
7. Production hardening has been applied.
8. CI/CD and deployment are established.
9. AI recommendations are integrated behind a provider abstraction.

Until then, prioritize completing the roadmap over adding unrelated features.

---

**END OF FINAL ROADMAP.**
