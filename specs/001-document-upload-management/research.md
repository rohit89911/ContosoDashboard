# Phase 0 Research: Document Upload and Management

**Note on method**: Unlike a typical Phase 0 (exploring open technology choices), most decisions
for this feature are **not** open choices — they are fixed by the approved spec (traced to the
stakeholder document) or by the constitution (traced to the existing codebase). This research
therefore mostly *confirms* those fixed decisions against the existing codebase and flags true
unknowns as **DEFERRED** rather than resolving them, per instruction #4 ("Do NOT silently resolve
any [NEEDS CLARIFICATION] item") and Constitution Principle IV.

## Confirmed (not open) decisions

### File storage location and abstraction

- **Decision**: Local filesystem, outside `wwwroot`, behind an `IFileStorageService` interface;
  `LocalFileStorageService` is the only implementation built now.
- **Rationale**: TC-001/TC-004, directly mandated by the stakeholder document's "Offline Storage
  Pattern" and "Azure Migration Design" sections. Not a choice made during planning.
- **Alternatives considered**: None — this is a fixed constraint, not an open question.

### Physical file naming and path convention

- **Decision**: GUID-based filename; path pattern `{userId}/{projectId-or-"personal"}/
  {guid}.{extension}`.
- **Rationale**: TC-002, verbatim from the stakeholder document (security: prevents path
  traversal and filename collisions).
- **Alternatives considered**: None — fixed constraint.

### Upload sequence

- **Decision**: Generate unique path → save file to disk → save metadata to database.
- **Rationale**: TC-003, verbatim from the stakeholder document, verified directly against the
  source text during the spec review (not from a paraphrase). This is retained exactly; planning
  does not alter it even though it introduces the orphaned-file edge case (see Deferred below).
- **Alternatives considered**: Database-record-first was considered and explicitly rejected —
  not because it's technically worse, but because it contradicts the confirmed stakeholder
  source, which this project's constitution (Principle III) requires deferring to.

### Primary key type and Category representation

- **Decision**: `Document.DocumentId` is `int` (EF Core default `IDENTITY` surrogate key, matching
  `User.UserId`, `Project.ProjectId`, etc. exactly); `Category` is a plain `string` column, not an
  enum.
- **Rationale**: TC-005/TC-006, verbatim stakeholder constraints, and independently consistent
  with every existing entity's key style (Constitution Principle VI).
- **Alternatives considered**: GUID keys and an enum `Category` were both explicitly rejected —
  not for technical reasons, but because the stakeholder document explicitly requires the
  opposite of both.

### Authorization approach for new document operations

- **Decision**: Reuse the existing service-layer `requestingUserId`-comparison pattern (as in
  `ProjectService.GetProjectByIdAsync`, `NotificationService.MarkAsReadAsync`,
  `UserService.UpdateUserProfileAsync`) inside `DocumentService`, rather than introducing policy-
  based `[Authorize(Policy=...)]` attributes or a new authorization framework.
- **Rationale**: Constitution Principle VI requires preserving this existing convention rather
  than introducing a parallel one; the existing pattern already satisfies FR-016/FR-017's
  role/ownership rules structurally (it just needs new comparisons for document ownership and
  project membership, which are additions of data, not of pattern).
- **Alternatives considered**: A generic `IAuthorizationHandler`-based policy system was
  considered and rejected as an unjustified parallel architecture, per Principle VI, since the
  existing per-service explicit-check pattern already covers every equivalent case elsewhere in
  the app.

### Serving files outside `wwwroot`

- **Decision**: A minimal MVC controller (`DocumentsController`) with two `[Authorize]` actions
  (download, preview-stream), added alongside the existing Razor Pages/Blazor Server pages.
- **Rationale**: The stakeholder document explicitly states this requirement ("Files stored
  outside `wwwroot` require controller endpoints to serve them (enables authorization checks)").
  Logged formally in `plan.md`'s Complexity Tracking table since it is a new pattern for this
  codebase, but it is source-required, not a default convenience choice.
- **Alternatives considered**: A Blazor Server component streaming the file directly was
  considered and rejected — Blazor Server's SignalR-based rendering pipeline is not designed to
  stream large binary HTTP responses with correct headers; an HTTP endpoint is the standard
  mechanism ASP.NET Core provides for this and is what the stakeholder document itself specifies.

### Notification integration

- **Decision**: Extend the existing `NotificationType` enum with new document-related values
  (e.g., document shared, new project document) and reuse `INotificationService.CreateNotificationAsync`
  as-is; no new notification delivery mechanism is introduced.
- **Rationale**: FR-020; existing notification infrastructure (`Notification` entity,
  `INotificationService`) already supports everything needed structurally — only new enum values
  are required.
- **Alternatives considered**: A separate document-specific notification table was considered and
  rejected as an unjustified parallel structure, per Principle VI.

## Deferred (NOT resolved by this plan) — five open decisions carried forward unchanged from the spec

- **Decision: DEFERRED — Department claim.** Whether adding a `Department` claim to the login
  flow (`Pages/Login.cshtml.cs`) is in scope for this feature, or a separate authentication change,
  is not decided here. **Impact if unresolved at implementation time**: any FR/story requiring
  "team-based" sharing authorization by department cannot be fully implemented until this is
  answered.
- **Decision: DEFERRED — Virus scanning engine/library.** FR-007 requires scanning, but no
  engine/library is chosen. **Impact if unresolved**: the upload pipeline's scan step
  (`DocumentService`) cannot be implemented past a placeholder/stub until this is answered.
- **Decision: DEFERRED — Automated testing approach.** Whether this feature introduces a test
  project (and what kind) is not decided. **Impact if unresolved**: `/speckit-tasks` cannot
  generate meaningful testing tasks beyond "decide this first."
- **Decision: DEFERRED — Task attachment mechanics.** The exact data model/UI for attaching a
  document to a task (FR-018) is not decided (e.g., whether `Document.ProjectId` alone suffices,
  or a dedicated task-document join entity is needed). **Impact if unresolved**: the `Tasks.razor`
  changes and any task-document association entity in `data-model.md` are described only at the
  level the stakeholder document supports, not fully specified.
- **Decision: DEFERRED — Orphaned file cleanup.** What happens when a file is saved to disk but
  the subsequent database insert fails (TC-003's un-addressed reverse-failure case) is not
  decided. **Impact if unresolved**: `DocumentService`'s upload method can be implemented per the
  confirmed sequence, but its failure-path behavior for this specific case is not fully specified
  until this is answered.

None of these five are resolved with an assumed default. Each remains a `[NEEDS CLARIFICATION]`
item in the spec and is carried forward into `tasks.md` (when that phase runs) as an explicit
prerequisite/decision task rather than silently defaulted.
