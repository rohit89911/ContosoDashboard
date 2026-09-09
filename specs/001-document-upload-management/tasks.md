---

description: "Task list template for feature implementation"
---

# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/001-document-upload-management/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md,
data-model.md, contracts/

**Tests**: No test tasks are generated. Automated Testing Approach is Open Decision #3
(unresolved) — generating concrete test-writing tasks here would silently resolve it by assuming
a framework/scope. See "Open Decision Dependencies" below for the placeholder decision task.

**Organization**: Tasks are grouped by user story (from spec.md, P1→P4) to enable independent
implementation and testing of each story, per Constitution Principle I (Spec-Driven Development)
and Principle VI (preserve existing brownfield conventions — all new code follows the
interface+impl-per-file, Scoped DI, direct `ApplicationDbContext`, service-layer
`requestingUserId` conventions documented in plan.md).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US7, matching spec.md)
- Every task includes the exact file path(s) it touches

## Path Conventions

Single existing project, extended in place: `ContosoDashboard/` at the repository root (no
`src/`/`backend/`/`frontend/` split — see plan.md "Structure Decision"). All paths below are
relative to `ContosoDashboard/` unless stated otherwise.

---

## Phase 1: Setup

**Purpose**: Minimal environment/config groundwork — this is a brownfield project being
extended, not a new project being initialized, so there is no scaffolding to create.

- [ ] T001 Verify local environment: `dotnet --version` reports a .NET 8 SDK is available (8.0.425
      confirmed present on this machine alongside 10.0.401), and SQL Server LocalDB is installed
      and startable (`sqllocaldb info`). No code changes.
- [ ] T002 Add a configurable local storage root path to `appsettings.json`
      (e.g. `"DocumentStorage": { "RootPath": "AppData/uploads" }`), matching the stakeholder
      document's example path and TC-001 (outside `wwwroot`).
- [ ] T003 [P] Add the configured storage root directory (e.g. `AppData/uploads/`) to
      `.gitignore` at the repository root — uploaded file content is runtime data, not source, and
      must not be committed (consistent with Constitution Principle V's version-control
      cleanliness, applied here to generated data rather than AI artifacts).

**Checkpoint**: Environment confirmed, configuration key present. No entities, services, or pages
exist yet.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure required by every user story — data model, storage abstraction,
and the business-logic service skeleton. **No user story task may start before this phase is
complete.**

- [ ] T004 [P] Create `Document` entity in `Models/Document.cs` per data-model.md, with EF Core
      Data Annotations exactly as specified: `DocumentId` (`int`, PK), `Title`
      (`[Required][MaxLength(255)]`), `Description` (`[MaxLength(2000)]`, optional), `Category`
      (`[Required][MaxLength(100)]`, free text — TC-006, NOT an enum), `FileName`
      (`[Required][MaxLength(255)]`), `FilePath` (`[Required][MaxLength(500)]`), `FileType`
      (`[Required][MaxLength(255)]` — TC-007), `FileSizeBytes` (`long`, required),
      `UploadedByUserId` (`int`, FK), `ProjectId` (`int?`, FK), `Tags` (`[MaxLength(500)]`,
      optional), `UploadedDate`/`UpdatedDate` (`DateTime`).
- [ ] T005 [P] Create `DocumentActivityLog` entity in `Models/DocumentActivityLog.cs` per
      data-model.md: `LogId` (`int`, PK), `DocumentId` (`int`, FK), `UserId` (`int`, FK),
      `ActionType` (`enum`: `Upload`, `Download`, `Delete`, `Share`), `Timestamp` (`DateTime`).
      Needed starting at US1 (upload logging, FR-021).
- [ ] T006 [P] Add new `NotificationType` enum values to `Models/Notification.cs` for document
      events (e.g. `DocumentShared`, `NewProjectDocument`) — schema of the `Notification` table is
      unchanged, only the enum gains values. Needed starting at US5/US6 (FR-020) but the enum
      itself has no dependents until then, so it is safe to add now alongside the other model
      work.
- [ ] T007 Register `Document` and `DocumentActivityLog` as `DbSet`s in
      `Data/ApplicationDbContext.cs`; add their relationships/foreign keys, and indexes
      (`Document.UploadedByUserId`, `Document.ProjectId`, `Document.Category`,
      `DocumentActivityLog.DocumentId`+`Timestamp`, `DocumentActivityLog.UserId`+`Timestamp`) in
      `OnModelCreating`, following the exact style already used there for `TaskItem`/`Project`
      indexes. Depends on T004, T005. **Do NOT add an EF Core migration** — the app creates its
      schema via `context.Database.EnsureCreated()` in `Program.cs`; this task only extends the
      model that `EnsureCreated()` will pick up (Constitution Principle VI).
- [ ] T008 Create `IFileStorageService`/`LocalFileStorageService` in
      `Services/LocalFileStorageService.cs` (interface + implementation in the same file, per the
      existing convention in e.g. `ProjectService.cs`): `UploadAsync(Stream, fileName,
      contentType)`, `DeleteAsync(filePath)`, `DownloadAsync(filePath)`, `GetUrlAsync(filePath,
      expiration)` (TC-004). `LocalFileStorageService` implements the path convention
      `{userId}/{projectId-or-"personal"}/{guid}.{extension}` (TC-002) under the
      `DocumentStorage:RootPath` configured in T002, using `System.IO` only — no cloud SDK
      dependency. Depends on T002.
- [ ] T009 Create `IDocumentService`/`DocumentService` skeleton in `Services/DocumentService.cs`
      (interface + implementation in the same file, per existing convention), constructor-injecting
      `ApplicationDbContext` and `IFileStorageService` directly (no repository layer — Constitution
      Principle VI). Method signatures only at this stage for: upload, get-my-documents,
      get-project-documents, search, download-authorize-check, update-metadata, replace-file,
      delete, share. Bodies are implemented per-story in later phases. Depends on T007, T008.
- [ ] T010 Register `IFileStorageService`→`LocalFileStorageService` and
      `IDocumentService`→`DocumentService` as `AddScoped` in `Program.cs`, matching the existing
      registration style for `IProjectService`/`INotificationService`/etc. Depends on T008, T009.

**Checkpoint**: Data model, storage abstraction, and service skeleton exist. No user-facing
behavior yet — user story phases below add it.

---

## Phase 3: User Story 1 - Upload and view my own documents (Priority: P1) 🎯 MVP

**Goal**: An employee can upload a file with required metadata and see it in "My Documents".

**Independent Test**: Log in, upload a PDF under 25MB with a title and category, confirm it
appears in "My Documents" with correct metadata (spec.md Acceptance Scenario US1-1).

### Implementation for User Story 1

- [ ] T011 [US1] Implement `DocumentService.UploadAsync` in `Services/DocumentService.cs`:
      validate file size ≤ 25MB (FR-003) and MIME type is one of PDF/Word/Excel/PowerPoint/
      text/JPEG/PNG (FR-002), validate `Title` is non-empty and `Category` is provided (FR-005),
      then execute the upload sequence **exactly** as TC-003 specifies and as verified against the
      stakeholder document during spec review — **generate the unique GUID-based path → save the
      file to disk via `IFileStorageService.UploadAsync` → save the `Document` metadata record to
      the database** — in that order, no other order. On success, write a `DocumentActivityLog`
      row with `ActionType = Upload` (FR-021). **Failure-path note (Open Decision: Orphaned File
      Cleanup — BLOCKED, do not resolve here):** implement only the approved file-first
      sequence for the success path. What happens if the file save succeeds but the subsequent
      database insert then fails is an unresolved decision — do NOT implement any specific
      behavior for that path as if it were approved, including logging-and-rethrowing framed as
      a chosen strategy, deleting the orphaned file, retrying, or any other
      compensation/rollback action. Treat post-DB-failure handling itself as a blocked
      placeholder (comparable to T036) pending a decision, not an implemented behavior.
      Depends on T009.
- [ ] T012 [US1] Implement `DocumentService.GetMyDocumentsAsync(int userId)` in
      `Services/DocumentService.cs`, returning documents where `UploadedByUserId == userId`
      (FR-008). Depends on T009.
- [ ] T013 [US1] Create `Pages/Documents.razor`: `@attribute [Authorize]`, `@inject
      IDocumentService`, read the current user id from `ClaimTypes.NameIdentifier` via
      `AuthenticationStateProvider` in `OnInitializedAsync` — matching the exact pattern in
      `Pages/ProjectDetails.razor` — and render an upload form (title + category required,
      description/project/tags optional per FR-005) using the `InputFile` component with the
      `MemoryStream` copy pattern the stakeholder document specifies (extract
      name/size/contentType into locals before opening the stream, copy to `MemoryStream`
      immediately, null out the `IBrowserFile` reference after copying, use `@key` on `InputFile`
      to force re-render after a successful upload) to avoid Blazor Server stream-disposal
      issues. Shows an upload progress indicator and a success/error message (FR-004). Depends on
      T011.
- [ ] T014 [US1] In `Pages/Documents.razor`, render the "My Documents" list from
      `GetMyDocumentsAsync`, showing title, category, upload date, file size, and associated
      project (FR-008). Depends on T012, T013.
- [ ] T015 [US1] Add a "Documents" link to `Shared/NavMenu.razor`, following the existing
      `NavLink`/Bootstrap-icon style used for Tasks/Projects/Team.

**Checkpoint**: Upload → view in "My Documents" works end-to-end and is independently testable
(spec.md quickstart.md scenario), without any other user story implemented.

---

## Phase 4: User Story 2 - Organize, browse, and search documents (Priority: P2)

**Goal**: Sort/filter "My Documents"; view a project's documents; search across accessible
documents.

**Independent Test**: Upload several documents (US1), then sort/filter the list and run a search,
independent of sharing/preview/integration (spec.md Acceptance Scenarios US2-1/2/3).

### Implementation for User Story 2

- [ ] T016 [US2] Extend `DocumentService.GetMyDocumentsAsync` in `Services/DocumentService.cs`
      with sort (`title`/`uploadDate`/`category`/`fileSize`) and filter (`category`/`project`/
      `dateRange`) parameters (FR-008). Depends on T012.
- [ ] T017 [US2] Implement `DocumentService.GetProjectDocumentsAsync(int projectId, int
      requestingUserId)` in `Services/DocumentService.cs`: return documents where `ProjectId`
      matches, but only after checking `requestingUserId` is the project's manager or a project
      member — reusing the exact authorization pattern in
      `ProjectService.GetProjectByIdAsync` (return an empty/unauthorized result rather than
      leaking documents to non-members) (FR-009). Depends on T009.
- [ ] T018 [US2] Add a project documents section to `Pages/ProjectDetails.razor`, calling
      `GetProjectDocumentsAsync`, with an upload action visible only to the project's manager
      (FR-009). Depends on T017.
- [ ] T019 [US2] Implement `DocumentService.SearchDocumentsAsync(string query, int
      requestingUserId)` in `Services/DocumentService.cs`: match against title, description, tags,
      uploader name, and project name, scoped to documents `requestingUserId` is authorized to see
      (own documents, project documents where a member, or all documents if Administrator per
      FR-016) — target: results within 2 seconds (SC-008). Depends on T009.
- [ ] T020 [US2] Add sort/filter controls and a search box to `Pages/Documents.razor`, wired to
      T016/T019.

**Checkpoint**: Browsing, filtering, and search work independently of download/preview/sharing.

---

## Phase 5: User Story 3 - Download and preview documents (Priority: P2)

**Goal**: Download any authorized document; preview PDF/image documents in-browser.

**Independent Test**: Download an uploaded document; separately, preview a PDF/image document
in-browser (spec.md Acceptance Scenarios US3-1/2).

### Implementation for User Story 3

- [ ] T021 [US3] Create `Controllers/DocumentsController.cs` (new folder — justified in plan.md
      Complexity Tracking: files are stored outside `wwwroot` per TC-001 and require an authorized
      HTTP endpoint to serve them, per the stakeholder document's own security section). Add
      `[Authorize]` at the controller level.
- [ ] T022 [US3] Implement the `GET /documents/{documentId}/download` action in
      `Controllers/DocumentsController.cs` per contracts/document-file-endpoints.md: call a new
      `DocumentService` authorization check (requesting user must own the document, be a member/
      manager of its project, or be an Administrator — same pattern as T017), stream the file via
      `IFileStorageService.DownloadAsync(document.FilePath)`, set `Content-Type` from
      `Document.FileType` and `Content-Disposition: attachment; filename="{Document.FileName}"`
      (the original display name — never the physical GUID path, per TC-002). Record a
      `DocumentActivityLog` row with `ActionType = Download` (FR-021). Return 404 for both
      "not found" and "not authorized" (do not distinguish). Depends on T021.
- [ ] T023 [US3] Implement the `GET /documents/{documentId}/preview` action in
      `Controllers/DocumentsController.cs` per contracts/document-file-endpoints.md: same
      authorization as T022, restricted to PDF/JPEG/PNG `FileType`s (reject others), stream via
      `IFileStorageService.DownloadAsync` with `Content-Type` set but **no**
      `Content-Disposition: attachment` header (inline rendering). Depends on T021.
- [ ] T024 [US3] Add "Download" and, where the file type qualifies, "Preview" actions/links to the
      document rows rendered in `Pages/Documents.razor` and the project documents section added to
      `Pages/ProjectDetails.razor` (T018), pointing at the T022/T023 endpoints.

**Checkpoint**: Download and preview work independently, gated by the same authorization pattern
used everywhere else in the app.

---

## Phase 6: User Story 4 - Edit metadata, replace, and delete documents (Priority: P3)

**Goal**: Uploader can edit metadata/replace file; uploader or Project Manager can delete.

**Independent Test**: Upload a document (US1), edit its title, replace its file, delete it
(spec.md Acceptance Scenarios US4-1/2/3/4).

### Implementation for User Story 4

- [ ] T025 [US4] Implement `DocumentService.UpdateMetadataAsync(Document, int requestingUserId)`
      in `Services/DocumentService.cs`: only the document's `UploadedByUserId` may edit
      Title/Description/Category/Tags (FR-013) — same explicit-comparison authorization style as
      `UserService.UpdateUserProfileAsync`. Depends on T009.
- [ ] T026 [US4] Implement `DocumentService.ReplaceFileAsync(int documentId, Stream, fileName,
      contentType, int requestingUserId)` in `Services/DocumentService.cs`: uploader-only (same
      check as T025); apply the **same TC-003 file-first sequence** for the new file (generate new
      path → save new file → update the `Document` record), then delete the old physical file via
      `IFileStorageService.DeleteAsync` only **after** the database update succeeds (so a failure
      partway through never leaves the document pointing at a missing file — this ordering choice
      is a direct application of TC-003's own rationale to the replace case, not a new invented
      rule). What happens if saving the new file succeeds but the database update then fails
      is the same unresolved Orphaned File Cleanup decision as T011 — do NOT implement any
      specific behavior for that failure path (no log-and-rethrow framed as approved, no delete,
      no retry); treat it as a blocked placeholder identically to T011. Depends on T009, T008.
- [ ] T027 [US4] Implement `DocumentService.DeleteDocumentAsync(int documentId, int
      requestingUserId)` in `Services/DocumentService.cs`: authorized if `requestingUserId` is the
      uploader, or is the Project Manager of the document's project (FR-014). Permanently removes
      the DB record and the physical file. Records a `DocumentActivityLog` row with `ActionType =
      Delete` before the record itself is removed (so the log survives — matches the `Restrict`
      delete behavior chosen in data-model.md for `DocumentActivityLog.Document`). Depends on
      T009.
- [ ] T028 [US4] Add edit-metadata and replace-file UI (a modal or inline form) and a
      delete-with-confirmation control to `Pages/Documents.razor`, wired to T025/T026/T027, visible
      only when the current user is authorized (uploader, or — for delete — also the project's
      manager).

**Checkpoint**: Full document lifecycle (upload→edit→replace→delete) works independently.

---

## Phase 7: User Story 5 - Share documents with notifications (Priority: P3)

**Goal**: Document owner shares with specific users; recipients are notified and see it under
"Shared with Me".

**Independent Test**: Upload a document (US1), share it with a second user, confirm they get a
notification and see it under "Shared with Me" (spec.md Acceptance Scenario US5-1).

### Implementation for User Story 5

- [ ] T029 [US5] Create `DocumentShare` entity in `Models/DocumentShare.cs` per data-model.md:
      `DocumentShareId` (`int`, PK), `DocumentId`/`SharedWithUserId`/`SharedByUserId` (`int`, FK),
      `SharedDate` (`DateTime`). Register as a `DbSet` in `Data/ApplicationDbContext.cs` with a
      unique composite index on (`DocumentId`, `SharedWithUserId`) and `Cascade` delete from
      `Document` (a share has no meaning once its document is gone, per data-model.md), added in
      `OnModelCreating` — no migration, `EnsureCreated()` picks this up.
- [ ] T030 [US5] Implement `DocumentService.ShareDocumentAsync(int documentId, int
      sharedWithUserId, int requestingUserId)` in `Services/DocumentService.cs`: only the
      document's uploader (owner) may share it (FR-015); reject if a `DocumentShare` for the same
      pair already exists (matches the unique index in T029). On success, call the existing
      `INotificationService.CreateNotificationAsync` with the new `NotificationType.DocumentShared`
      value (from T006) — no new notification delivery mechanism. Record a
      `DocumentActivityLog` row with `ActionType = Share` (FR-021). Depends on T029, T006.
- [ ] T031 [US5] Implement `DocumentService.GetSharedWithMeAsync(int userId)` in
      `Services/DocumentService.cs`, returning documents shared with `userId` via `DocumentShare`.
      Depends on T029.
- [ ] T032 [US5] Add a "Shared with Me" section/tab to `Pages/Documents.razor` (T031) and a
      "Share" action on document rows the current user owns, prompting for a recipient user
      (T030).

**Checkpoint**: Sharing + notification works independently of task/dashboard integration.

---

## Phase 8: User Story 6 - Integrate with tasks and the dashboard (Priority: P3)

**Goal**: Recent Documents dashboard widget + doc count; project-document notifications; task
attachment (mechanics open).

**Independent Test**: Dashboard shows the 5 most recent uploads and a document count (spec.md
Acceptance Scenario US6-2); a project member is notified when a new document is added to their
project (US6-3).

### Implementation for User Story 6

- [ ] T033 [US6] Implement `DocumentService.GetRecentDocumentsAsync(int userId, int count = 5)` in
      `Services/DocumentService.cs` (FR-019). Depends on T009.
- [ ] T034 [US6] Add a "Recent Documents" widget to `Pages/Index.razor` using T033, and a document
      count to the existing dashboard summary cards (reusing/extending `DashboardService`'s
      existing summary-card pattern in `Services/DashboardService.cs`) (FR-019).
- [ ] T035 [US6] In `DocumentService.UploadAsync` (T011), when the uploaded document has a
      non-null `ProjectId`, notify each project member (via `ProjectMember` lookup, matching
      `ProjectService`'s membership-query pattern) using `INotificationService
      .CreateNotificationAsync` with `NotificationType.NewProjectDocument` (FR-020). Depends on
      T011, T006.
- [ ] T036 [US6] **BLOCKED — Open Decision: Task Attachment Mechanics.** Do not implement task
      attachment in `Pages/Tasks.razor` (or a task-detail page) until the underlying mechanism is
      decided (e.g., whether `Document.ProjectId` alone suffices, or a dedicated task-document
      association entity is required, and what the attach/upload UI on the task detail page looks
      like — none of this is specified by the stakeholder document). This task is a placeholder
      to be replaced with concrete sub-tasks once the decision is made; do not invent a data model
      or UI for it now. FR-018 remains unimplemented until this decision is resolved.

**Checkpoint**: Dashboard widget and project-document notifications are independently functional;
task attachment (FR-018) is explicitly deferred, not silently implemented with a guessed design.

---

## Phase 9: User Story 7 - Audit logging and admin reporting (Priority: P4)

**Goal**: Administrators can generate reports from the activity log already being written by
US1/US3/US4/US5's tasks.

**Independent Test**: Perform uploads/downloads/deletions/shares (earlier stories), then confirm
an Administrator can generate a report reflecting those actions (spec.md Acceptance Scenario
US7-1).

### Implementation for User Story 7

- [ ] T037 [US7] Implement `DocumentService.GetDocumentActivityReportAsync()` in
      `Services/DocumentService.cs`, querying `DocumentActivityLog` (written by T011/T022/T027/
      T030) for: most-uploaded document types (group by `FileType`), most active uploaders (group
      by `UserId` where `ActionType == Upload`), and document access patterns (counts by
      `ActionType` over time) (FR-022).
- [ ] T038 [US7] Create an Administrator-only reporting page (e.g. `Pages/DocumentReports.razor`)
      gated by the **existing** `Administrator` authorization policy already defined in
      `Program.cs` (`options.AddPolicy("Administrator", policy =>
      policy.RequireRole("Administrator"))`) — reusing that existing policy rather than inventing
      a new authorization mechanism — rendering T037's report. Depends on T037.

**Checkpoint**: Reporting works, backed entirely by logging already wired into earlier stories'
tasks — no new logging plumbing introduced here.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Validation and final checks that span multiple stories.

- [ ] T039 Run through every scenario in `quickstart.md` manually against the running application
      (`dotnet run` from `ContosoDashboard/`) and confirm each passes.
- [ ] T040 [P] Re-verify Constitution Principle VI compliance across all new files: interface+impl
      colocated per service file, `AddScoped` registrations, service-layer `requestingUserId`
      checks present on every document operation, no repository/unit-of-work layer introduced, no
      EF Core migration added.
- [ ] T041 [P] Confirm no task above introduced SQLite, another database engine, a cloud/blob
      storage implementation, or any dependency not already present in `ContosoDashboard.csproj`
      plus plain `System.IO` (per Technical Context / research.md).

---

## Open Decision Dependencies

*(Per explicit instruction: none of these five decisions is resolved by this task list. Tasks that
touch them are marked BLOCKED or carry an explicit no-op/safe-default note rather than an invented
resolution.)*

| # | Open Decision | Affected Task(s) | Effect |
|---|---|---|---|
| 1 | Department claim in login flow | None directly implemented — no task in this list adds it. If resolved later, any task needing department-based sharing/authorization (not present in the current FR set beyond what's already covered by project membership) would be added then. | No current task blocked; flagged so it is not silently assumed absent-forever either. |
| 2 | Virus scanning engine/library | T011 (`UploadAsync`) | T011 must NOT select or call a scanning library. Implement the upload flow with an explicit stub/extension point (e.g. a clearly-commented no-op call site) so scanning can be added later without re-architecting T011 — do not skip mentioning FR-007 as unimplemented. |
| 3 | Automated testing approach | Entire tasks.md (see "Tests" note above) | No test-writing tasks exist. `/speckit-tasks` deliberately generated none rather than assuming a framework/scope. |
| 4 | Task attachment mechanics | T036 | Explicitly BLOCKED — no sub-tasks generated; FR-018 is unimplemented until resolved. |
| 5 | Orphaned file cleanup (file saved, DB insert fails) | T011, T026 | Both tasks implement only the approved file-first success sequence. The post-DB-failure handling itself is treated as a BLOCKED placeholder, not implemented as any specific behavior — no log-and-rethrow, delete, retry, or compensation logic is chosen here. |

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup (T002 before T008). **Blocks all user stories.**
- **User Stories (Phase 3-9)**: All depend on Foundational (Phase 2) completion.
  - US1 (P1): no dependency on other stories — the MVP slice.
  - US2 (P2): depends on US1's `DocumentService`/`Documents.razor` existing (T012/T013) to extend
    rather than duplicate.
  - US3 (P2): independent of US2; only depends on Foundational (needs `Document`/
    `IFileStorageService` from Phase 2) and the authorization pattern established in US2's T017.
  - US4 (P3): depends on US1 (extends `Documents.razor` and `DocumentService`) but is
    independently testable per its own acceptance scenarios.
  - US5 (P3): depends on Foundational's T006 (notification enum) and US1's upload flow existing.
  - US6 (P3): depends on US1 (`GetRecentDocumentsAsync`/notification-on-upload extend
    `UploadAsync`) — T036 (task attachment) has no dependencies since it is blocked/unimplemented.
  - US7 (P4): depends on US1/US3/US4/US5 already writing `DocumentActivityLog` rows (T011, T022,
    T027, T030) — there is nothing to report on otherwise.
- **Polish (Phase 10)**: Depends on all implemented user stories.

### Parallel Opportunities

- T004, T005, T006 (entity/enum creation) are `[P]` — different files, no cross-dependency.
- T003 is `[P]` relative to T001/T002 (different concern, `.gitignore` vs. environment/config).
- Within US1: none of T011-T015 are marked `[P]` — each depends on the previous (service before
  page before nav link).
- T040, T041 are `[P]` relative to each other and to T039 (review tasks touching different
  concerns, no shared file).

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 (Setup) → Phase 2 (Foundational) → Phase 3 (US1).
2. **STOP and VALIDATE**: run the quickstart.md US1 scenario.
3. This delivers: "users can upload and view their documents" (T001-T015, 15 tasks).

### Incremental Delivery

Setup + Foundational → US1 (MVP) → US2 → US3 → US4 → US5 → US6 (minus the blocked task
attachment) → US7 → Polish. Each story's Checkpoint above confirms independent testability before
moving on.

---

## Notes

- 41 tasks total (T001-T041) across Setup, Foundational, 7 user stories, and Polish.
- No task instructs writing an EF Core migration; all schema changes rely on the existing
  `EnsureCreated()` bootstrap in `Program.cs`, per Constitution Principle VI.
- No task introduces a repository/unit-of-work layer, a second database engine, or a cloud storage
  implementation.
- The post-DB-failure paths in T011 and T026 (Orphaned File Cleanup) and all of T036 (Task
  Attachment Mechanics) are treated as BLOCKED placeholders, not implemented behavior. Do not
  remove these blocked markers when implementing — they are the record that these decisions
  were deliberately deferred, not missed.
