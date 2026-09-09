# Feature Specification: Document Upload and Management

**Feature Branch**: `001-document-upload-management`

**Created**: 2026-09-09

**Status**: Draft

**Input**: User description: "Enable Contoso employees to upload work-related documents, organize
them by category and project, and share them with team members, per
`StakeholderDocs/document-upload-and-management-feature.md`, implemented as a feature of the
existing ContosoDashboard brownfield application."

**Governing principles**: `.specify/memory/constitution.md` v1.1.0 (Spec-Driven Development;
confirmed technology stack; no invented requirements; explicit decision logging; preserve existing
brownfield conventions).

**Primary source**: `StakeholderDocs/document-upload-and-management-feature.md` (cited below as
"Stakeholder Doc §<section>"). Every functional requirement is traced either to that document or
to verified existing code (cited as "Existing Code: <file>").

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Upload and view my own documents (Priority: P1)

An employee selects one or more files from their computer, supplies a title and category (and
optionally a description, related project, and tags), uploads them, and then sees the documents
listed in a "My Documents" view.

**Why this priority**: This is the minimum slice that delivers value on its own — without it, no
other document capability (organization, sharing, search, integration) has anything to operate on.
(Stakeholder Doc §"Core Requirements" 1 and 2, "My Documents View".)

**Independent Test**: Can be fully tested by logging in as an employee, uploading a single PDF
under 25 MB with a title and category, and confirming it appears in "My Documents" with correct
metadata — no other user story is required for this to be verifiable.

**Acceptance Scenarios**:

1. **Given** an employee is logged in, **When** they select a PDF file under 25 MB, provide a
   title and choose a category, and submit the upload, **Then** the document uploads successfully,
   a progress indicator is shown during the upload, and the document appears in their "My
   Documents" list with title, category, upload date, file size, and uploader displayed. (Stakeholder
   Doc §"File Selection and Upload", §"Document Metadata", §"My Documents View")
2. **Given** an employee attempts to upload a file larger than 25 MB, **When** they submit the
   upload, **Then** the system rejects it and displays an error message stating the 25 MB limit.
   (Stakeholder Doc §"Validation and Security")
3. **Given** an employee attempts to upload a file of an unsupported type (e.g., `.exe`), **When**
   they submit the upload, **Then** the system rejects it with a clear error message naming the
   supported types. (Stakeholder Doc §"Validation and Security")
4. **Given** an employee submits an upload without a title or without selecting a category, **When**
   they submit, **Then** the system blocks the upload and indicates which required field is
   missing. (Stakeholder Doc §"Document Metadata": title and category are required)

---

### User Story 2 - Organize, browse, and search documents (Priority: P2)

An employee views all documents associated with a specific project (if they are a member), sorts
and filters their own document list, and searches across documents they have permission to see.

**Why this priority**: Once documents can be uploaded (US1), the next most valuable capability is
finding them again — this is the stated business problem in the Stakeholder Doc's "Business Need"
section.

**Independent Test**: Can be tested by uploading several documents with different categories/dates
(via US1), then filtering/sorting the "My Documents" list and running a search, independent of
sharing, preview, or integration features.

**Acceptance Scenarios**:

1. **Given** an employee has uploaded multiple documents, **When** they open "My Documents",
   **Then** they can sort by title, upload date, category, or file size, and filter by category,
   project, or date range. (Stakeholder Doc §"My Documents View")
2. **Given** an employee is a member of a project, **When** they view that project, **Then** they
   see all documents associated with the project and can download them. (Stakeholder Doc
   §"Project Documents View")
3. **Given** an employee searches by title, description, tag, uploader name, or project name,
   **When** they submit the search, **Then** results return within 2 seconds and include only
   documents the searching user has permission to access. (Stakeholder Doc §"Search")

---

### User Story 3 - Download and preview documents (Priority: P2)

A user downloads any document they have access to, and previews common file types (PDF, images)
directly in the browser without downloading.

**Why this priority**: Directly enables the core value of the feature (using the documents once
found), and is explicitly required alongside browsing (US2).

**Independent Test**: Can be tested by downloading an uploaded document and, separately, opening a
PDF or image document in the in-browser preview, independent of editing/sharing features.

**Acceptance Scenarios**:

1. **Given** a user has access to a document, **When** they choose to download it, **Then** the
   original file is delivered to them, subject to an authorization check that rejects the request
   if they do not have access. (Stakeholder Doc §"Download and Preview"; Existing Code:
   authorization pattern in `Services/ProjectService.cs`, `Services/NotificationService.cs`)
2. **Given** a user has access to a PDF or image document, **When** they choose to preview it,
   **Then** it renders in the browser within 3 seconds without requiring a separate download step.
   (Stakeholder Doc §"Download and Preview", §"Performance Requirements")

---

### User Story 4 - Edit metadata, replace, and delete documents (Priority: P3)

The uploader of a document edits its title/description/category/tags, replaces the underlying
file with a new version, or deletes it; a Project Manager may also delete any document within
their own projects.

**Why this priority**: Necessary for correcting mistakes and lifecycle management, but the feature
delivers value (upload, find, use) without it, so it is lower priority than US1-US3.

**Independent Test**: Can be tested by uploading a document (US1), then editing its title, then
replacing its file, then deleting it — independent of sharing or integration features.

**Acceptance Scenarios**:

1. **Given** a user uploaded a document, **When** they edit its title, description, category, or
   tags, **Then** the changes are saved and reflected in all views. (Stakeholder Doc §"Edit
   Metadata")
2. **Given** a user uploaded a document, **When** they replace the file, **Then** the new file
   supersedes the old one under the same document record. (Stakeholder Doc §"Edit Metadata")
3. **Given** a user uploaded a document, **When** they delete it and confirm the deletion,
   **Then** it is permanently removed. **Given** a Project Manager views a document in one of
   their own projects that they did not upload, **When** they delete it and confirm, **Then** it
   is permanently removed. (Stakeholder Doc §"Delete Documents")
4. **Given** an employee who neither uploaded a document nor manages its project, **When** they
   attempt to edit or delete it, **Then** the system denies the action. (Stakeholder Doc
   §"Access Management" role table; Existing Code: IDOR-protection convention)

---

### User Story 5 - Share documents with notifications (Priority: P3)

A document owner shares a document with specific users; recipients are notified in-app and can
find the document in a "Shared with Me" section.

**Why this priority**: Adds collaboration value on top of the core upload/organize/find loop, but
is not required for the feature to be independently useful.

**Independent Test**: Can be tested by uploading a document (US1), sharing it with a second test
user, and confirming that user receives a notification and can see the document under "Shared with
Me" — independent of task/dashboard integration.

**Acceptance Scenarios**:

1. **Given** a document owner shares a document with another user, **When** the share is saved,
   **Then** the recipient receives an in-app notification and the document appears in their
   "Shared with Me" section. (Stakeholder Doc §"Share Documents")

---

### User Story 6 - Integrate with tasks and the dashboard (Priority: P3)

Users view and attach documents from a task detail page (auto-associating the document with the
task's project), see a "Recent Documents" widget and document count on the dashboard home page,
and receive notifications when a new document is added to one of their projects.

**Why this priority**: Extends the feature's reach into existing workflows but depends on US1-US3
already working; the exact task-attachment data/UI mechanics are an open decision (see Open
Decisions below), so this story's acceptance scenarios are scoped to what the Stakeholder Doc
states explicitly.

**Independent Test**: Can be tested by uploading a document from a task detail page and confirming
it appears both attached to that task and in the project's document list; separately, confirming
the dashboard shows the 5 most recent documents and a document count.

**Acceptance Scenarios**:

1. **Given** a user is viewing a task, **When** they upload a document from the task detail page,
   **Then** the document is automatically associated with the task's project. (Stakeholder Doc
   §"Task Integration") — *exact attachment mechanism is an open decision; see Open Decisions.*
2. **Given** a user opens the dashboard home page, **When** the page loads, **Then** it shows a
   "Recent Documents" widget listing the last 5 documents the user uploaded, and a document count
   in the summary cards. (Stakeholder Doc §"Dashboard Integration")
3. **Given** a new document is added to a project a user belongs to, **When** the upload
   completes, **Then** the user receives an in-app notification. (Stakeholder Doc
   §"Notifications")

---

### User Story 7 - Audit logging and admin reporting (Priority: P4)

Administrators review reports on document activity (most uploaded types, most active uploaders,
access patterns), backed by a log of every upload, download, deletion, and share action.

**Why this priority**: A compliance/oversight capability layered on top of all other stories;
provides no end-user value on its own and can be delivered last without blocking earlier stories.

**Independent Test**: Can be tested by performing uploads/downloads/deletions/shares (from earlier
stories) as different users, then confirming an Administrator can generate a report reflecting
those actions.

**Acceptance Scenarios**:

1. **Given** document uploads, downloads, deletions, and shares have occurred, **When** an
   Administrator requests an activity report, **Then** the report shows the most uploaded document
   types, the most active uploaders, and document access patterns. (Stakeholder Doc §"Reporting
   and Audit")

---

### Edge Cases

- What happens when a user's session expires mid-upload? (Not specified by the Stakeholder Doc;
  treated as an assumption — see Assumptions.)
- What happens when disk storage is full during an upload? *(Explicitly out of scope for this
  spec per your review — remains a known gap, not resolved here; the Stakeholder Doc does not
  specify a required behavior.)*
- What happens when a file is successfully saved to disk (per TC-003's required file-first
  sequence) but the subsequent database insert fails, leaving an orphaned file with no metadata
  record? The Stakeholder Doc specifies the file-first order and why it avoids *orphaned database
  records*, but does not specify cleanup/rollback behavior for this reverse case. Not resolved
  here — see Open Decisions: Orphaned File Cleanup.
- What happens when two people attempt to edit the same document's metadata at the same time?
  (Not specified by the Stakeholder Doc; no concurrency requirement is stated, so no requirement is
  invented here — flagged as unaddressed.)
- What happens when a project a document is associated with is later deleted? (Not specified by
  the Stakeholder Doc; not addressed here.)
- What happens when the virus scan flags an uploaded file? Stakeholder Doc §"Validation and
  Security" requires scanning "before storage" but does not specify user-facing behavior on a
  positive detection — flagged as an open decision (see Open Decisions: Virus Scanning).

## Requirements *(mandatory)*

### Functional Requirements

**Upload**

- **FR-001**: Users MUST be able to select one or more files from their computer to upload in a
  single action. (Stakeholder Doc §"File Selection and Upload") — **Implementation status (final
  release-readiness review, 2026-09-09): PARTIAL, not satisfied** — the delivered upload form
  accepts one file per action only; see Open Decisions: Multi-File Upload Interaction Model.
- **FR-002**: The system MUST accept only PDF, Microsoft Office documents (Word, Excel,
  PowerPoint), plain text files, and images (JPEG, PNG), rejecting all other types with a clear
  error message. (Stakeholder Doc §"File Selection and Upload")
- **FR-003**: The system MUST reject any individual file larger than 25 MB with a clear error
  message stating the limit. (Stakeholder Doc §"File Selection and Upload")
- **FR-004**: The system MUST show a progress indicator during upload and a success or error
  message on completion. (Stakeholder Doc §"File Selection and Upload")
- **FR-005**: Users MUST supply a document title (required) and select a category from a
  predefined list (required: Project Documents, Team Resources, Personal Files, Reports,
  Presentations, Other) when uploading; description, associated project, and tags are optional.
  (Stakeholder Doc §"Document Metadata")
- **FR-006**: The system MUST automatically capture upload date/time, uploading user, file size,
  and file type (MIME type) for every uploaded document. (Stakeholder Doc §"Document Metadata")
- **FR-007**: The system MUST scan every uploaded file for viruses/malware before it is stored.
  (Stakeholder Doc §"Validation and Security") — *scanning engine/library is an open decision;
  see Open Decisions: Virus Scanning.*

**Organization & Browsing**

- **FR-008**: Users MUST be able to view a "My Documents" list of all documents they have
  uploaded, showing title, category, upload date, file size, and associated project; sortable by
  title, upload date, category, or file size; filterable by category, project, or date range.
  (Stakeholder Doc §"My Documents View")
- **FR-009**: When a user views a project they belong to, the system MUST show all documents
  associated with that project to every project member; Project Managers MUST additionally be
  able to upload documents to their own projects. (Stakeholder Doc §"Project Documents View")
- **FR-010**: Users MUST be able to search documents by title, description, tags, uploader name,
  or associated project, with results returned within 2 seconds and limited to documents the
  searching user is authorized to access. (Stakeholder Doc §"Search")

**Access & Management**

- **FR-011**: Users MUST be able to download any document they are authorized to access.
  (Stakeholder Doc §"Download and Preview")
- **FR-012**: For PDF and image documents, users MUST be able to preview the document in the
  browser without downloading it first, rendering within 3 seconds. (Stakeholder Doc §"Download
  and Preview", §"Performance Requirements")
- **FR-013**: A document's uploader MUST be able to edit its title, description, category, and
  tags, and MUST be able to replace the underlying file with an updated version. (Stakeholder Doc
  §"Edit Metadata")
- **FR-014**: A document's uploader MUST be able to permanently delete it after confirmation; a
  Project Manager MUST additionally be able to delete any document within a project they manage.
  (Stakeholder Doc §"Delete Documents")
- **FR-015**: A document owner MUST be able to share a document with specific users; recipients
  MUST receive an in-app notification and MUST see the document in a "Shared with Me" section.
  (Stakeholder Doc §"Share Documents")
- **FR-016**: The system MUST enforce role-based access so that: Employees can upload/manage
  documents they own or that belong to projects they're assigned to; Team Leads can additionally
  view/manage documents uploaded by their team members; Project Managers can additionally manage
  all documents in projects they manage; Administrators have full access to all documents for
  audit/compliance. (Stakeholder Doc §"Target Users") — **Implementation status (final
  release-readiness review, 2026-09-09): OPEN, not satisfied** — the Team Lead clause is not
  implemented (no data source defines "team members"); see Open Decisions: Team Lead "Team
  Membership" Definition.
- **FR-017**: Every document access/management operation MUST be authorized against the
  requesting user's identity and relationship to the document/project (ownership, project
  membership, or role), consistent with the existing IDOR-protection convention already used
  elsewhere in the application. (Stakeholder Doc §"Access Management" role table; Existing Code:
  `Services/ProjectService.cs`, `Services/NotificationService.cs`, `Services/UserService.cs` —
  each authorizes via an explicit `requestingUserId` comparison)

**Integration**

- **FR-018**: Users MUST be able to view and attach related documents from a task detail page;
  documents uploaded from a task MUST be automatically associated with that task's project.
  (Stakeholder Doc §"Task Integration") — *the precise attachment data/UI model is an open
  decision; see Open Decisions: Task Attachment.*
- **FR-019**: The dashboard home page MUST show a "Recent Documents" widget listing the current
  user's 5 most recently uploaded documents, and MUST include a document count in the dashboard
  summary cards. (Stakeholder Doc §"Dashboard Integration")
- **FR-020**: Users MUST receive an in-app notification when a document is shared with them, and
  when a new document is added to a project they belong to. (Stakeholder Doc §"Notifications";
  Existing Code: `Services/NotificationService.cs` already provides in-app notification
  infrastructure — new document-related `NotificationType` values are needed, since none exist
  today)

**Reporting & Audit**

- **FR-021**: The system MUST log every document upload, download, deletion, and share action.
  (Stakeholder Doc §"Activity Tracking")
- **FR-022**: Administrators MUST be able to generate reports showing the most uploaded document
  types, the most active uploaders, and document access patterns. (Stakeholder Doc §"Activity
  Tracking")

### Technical Constraints *(stakeholder-mandated, non-negotiable per Constitution Principle II)*

- **TC-001**: Uploaded files MUST be stored on the local filesystem, outside `wwwroot`, for this
  training implementation — no cloud storage is introduced now. (Stakeholder Doc §"Implementation
  Notes for Local File Storage"; Constitution Principle II)
- **TC-002**: Physical files MUST use GUID-based filenames (never the user-supplied filename) to
  prevent path traversal, following the path pattern `{userId}/{projectId-or-"personal"}/
  {guid}.{extension}`. (Stakeholder Doc §"Security Considerations", §"Implementation Approach")
- **TC-003**: The upload sequence MUST be: generate the unique file path → save the file to disk
  → save the metadata record to the database — in that order — to avoid orphaned database
  records if the file save fails and to avoid duplicate-key errors from empty/non-unique file
  paths. (Stakeholder Doc §"Implementation Notes for Local File Storage", verbatim: "Upload
  sequence: Generate unique path → Save file to disk → Save metadata to database" / "This
  prevents orphaned database records if file save fails" / "This prevents duplicate key errors
  from empty or non-unique file paths") — *the Stakeholder Doc specifies this exact order and
  its rationale (avoiding orphaned DB records and duplicate-key errors); it does **not** specify
  what should happen in the reverse failure case — a physical file successfully saved to disk
  followed by a failed database insert, which would leave an orphaned file on disk with no
  corresponding record. That cleanup/rollback behavior is not defined by any confirmed source and
  is not invented here; see Open Decisions: Orphaned File Cleanup.*
- **TC-004**: The system MUST implement an `IFileStorageService`-style interface abstraction
  (upload/delete/download/get-URL operations) so a future cloud storage implementation can be
  substituted via configuration, without changing business logic, controllers, or the database
  schema. Only the abstraction and its local-filesystem implementation are in scope now — no
  cloud implementation is built. (Stakeholder Doc §"Azure Migration Design"; Constitution
  Principle II — do not introduce cloud storage now)
- **TC-005**: The document identifier MUST be an integer surrogate key, consistent with existing
  `User`/`Project`/`TaskItem` keys — not a GUID. (Stakeholder Doc §"Technical Constraints";
  Constitution Principle VI — existing entities all use `int` keys)
- **TC-006**: Category MUST be stored as free text (e.g., "Project Documents", "Personal Files"),
  not as an integer enum. (Stakeholder Doc §"Technical Constraints")
- **TC-007**: The stored file-type (MIME type) field MUST accommodate values up to 255 characters,
  to fit long Office MIME type strings. (Stakeholder Doc §"Document Metadata", §"Implementation
  Approach")
- **TC-008**: New document-related functionality MUST follow the existing brownfield conventions
  already used across the codebase (service interface+implementation co-located per file, Scoped
  DI with direct `ApplicationDbContext` injection, service-layer `requestingUserId`-based
  authorization, EF Core configuration centralized in `OnModelCreating`) rather than introducing a
  parallel architecture. (Constitution Principle VI)

### Existing System Behavior & Dependencies *(verified from the imported codebase — not new requirements, but constraints this feature must operate within)*

- The application authenticates users via a mock cookie-based scheme; the authenticated user's id
  is available as `ClaimTypes.NameIdentifier`, name as `ClaimTypes.Name`, email as
  `ClaimTypes.Email`, and role as `ClaimTypes.Role`. **No `Department` claim is currently issued**,
  even though `User.Department` exists on the data model. (Existing Code:
  `Pages/Login.cshtml.cs`, `Program.cs`) — *whether this feature must add the `Department` claim
  is an open decision; see Open Decisions: Department Claim.*
- Existing roles are `Employee`, `TeamLead`, `ProjectManager`, `Administrator` (Existing Code:
  `Models/User.cs` — `UserRole` enum), matching the Stakeholder Doc's target-user roles exactly.
- Project membership is tracked via `ProjectMember` (ProjectId, UserId, Role as free text);
  project management is tracked via `Project.ProjectManagerId`. (Existing Code:
  `Models/ProjectMember.cs`, `Models/Project.cs`)
- Existing in-app notification infrastructure (`Notification` entity, `INotificationService`)
  already supports per-user notifications with priority and read/unread state, but its
  `NotificationType` enum has no document-related values yet. (Existing Code:
  `Models/Notification.cs`, `Services/NotificationService.cs`)
- The database is created via `EnsureCreated()` at startup; there is no EF Core migrations
  workflow in use today. (Existing Code: `Program.cs`)
- No automated test project exists anywhere in the repository today. (Verified: no `*Tests*`
  project found in the repository.)

### Key Entities *(include if feature involves data)*

- **Document**: Represents an uploaded file's metadata. Attributes: integer `DocumentId`
  (TC-005), `Title` (required), `Description` (optional), `Category` (required, free text,
  TC-006), `FileName` (original name as supplied by the user, for display only — never used as
  the physical path, per TC-002), `FilePath` (GUID-based physical path, TC-002), `FileType` (MIME
  type, up to 255 characters, TC-007), `FileSizeBytes`, `UploadedByUserId` (references the
  uploading `User`), `ProjectId` (optional — references `Project`; absent means a personal
  document), `Tags` (optional, for search), `UploadedDate`, `UpdatedDate`.
- **DocumentShare**: Represents a sharing relationship. Attributes: `DocumentShareId`,
  `DocumentId` (references `Document`), `SharedWithUserId` (references `User`), `SharedByUserId`
  (references `User`), `SharedDate`.
- **DocumentActivityLog**: Represents an audited action for reporting (FR-021, FR-022).
  Attributes: `LogId`, `DocumentId`, `UserId`, `ActionType` (Upload/Download/Delete/Share),
  `Timestamp`.
- **Task attachment relationship**: The Stakeholder Doc requires documents to be attachable from a
  task detail page and auto-associated with the task's project (FR-018), but does not specify
  whether a document can be attached to more than one task, or whether "attachment" is simply the
  existing `Document.ProjectId` association versus a dedicated task-document link. **No entity is
  defined for this here** — left open; see Open Decisions: Task Attachment.
- Existing entities this feature depends on but does not modify structurally: **User**, **Project**,
  **ProjectMember**, **TaskItem**, **Notification** (extended only with new `NotificationType`
  values per FR-020).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 70% of active ContosoDashboard users have uploaded at least one document within 3
  months of launch. (Stakeholder Doc §"Success Metrics")
- **SC-002**: Users can locate a specific document in under 30 seconds on average. (Stakeholder
  Doc §"Success Metrics", §"User Experience Goals")
- **SC-003**: 90% of uploaded documents are properly categorized (i.e., assigned a category other
  than a default/placeholder value). (Stakeholder Doc §"Success Metrics")
- **SC-004**: Zero security incidents related to unauthorized document access occur after launch.
  (Stakeholder Doc §"Success Metrics")
- **SC-005**: Uploading a document requires no more than 3 user actions/clicks from initiation to
  completion. (Stakeholder Doc §"User Experience Goals")
- **SC-006**: An upload of a file up to 25 MB completes within 30 seconds on a typical network
  connection. (Stakeholder Doc §"Performance Requirements")
- **SC-007**: A document list of up to 500 documents loads within 2 seconds. (Stakeholder Doc
  §"Performance Requirements")
- **SC-008**: A document search returns results within 2 seconds. (Stakeholder Doc §"Performance
  Requirements")
- **SC-009**: A document preview loads within 3 seconds. (Stakeholder Doc §"Performance
  Requirements")

## Assumptions

- Training environment has local disk storage available; most documents will be under 10 MB; the
  local filesystem is acceptable for this training implementation; cloud migration is a future,
  out-of-scope concern. (Stakeholder Doc §"Assumptions", verbatim)
- Users may work offline (no internet connection required for core document functionality).
  (Stakeholder Doc §"Assumptions", verbatim; also Constitution Principle II)
- Users are familiar with basic file management concepts (selecting/opening files on their own
  computer). (Stakeholder Doc §"Assumptions", verbatim)
- Session-expiry-during-upload behavior is not addressed by any confirmed source; this spec does
  not define required behavior for it (see Edge Cases).
- Concurrent metadata edits by two users are not addressed by any confirmed source; no locking or
  conflict-resolution requirement is asserted here.
- What happens to a document when its associated project is later deleted is not addressed by any
  confirmed source; no cascade/orphan-handling requirement is asserted here.

## Open Decisions Requiring Stakeholder/Product Input

*(Per explicit instruction, the first four items below are deliberately left unresolved rather
than answered with an assumed default. The fifth item was newly identified during the final
source-level audit of TC-003 and is left open for the same reason: no confirmed source defines
an answer, so none is invented. Items six and seven were newly identified during the final
release-readiness review of the delivered implementation against this spec (2026-09-09) — each
names a requirement whose delivered behavior does not fully satisfy its FR as written, for reasons
recorded below, and each is left open for the same reason as the first five: no confirmed source
resolves the underlying question, so none is invented or assumed here. This spec does not proceed
past documenting all seven.)*

- **[NEEDS CLARIFICATION — Department Claim]**: The Stakeholder Doc's implementation notes state
  that "Department claim is required for team-based authorization in document sharing" and that
  "missing claims will cause authorization failures," but the existing login flow
  (`Pages/Login.cshtml.cs`) does not issue a `Department` claim today. Is adding the `Department`
  claim to the authentication flow in scope for this feature, or is it a separate,
  independently-approved change to the authentication system?
- **[NEEDS CLARIFICATION — Virus Scanning Engine]**: FR-007 requires scanning every uploaded file
  for viruses/malware before storage, per the Stakeholder Doc, but no scanning engine, library, or
  service is named anywhere in the Stakeholder Doc or the existing codebase. What is the required
  scanning mechanism (e.g., a specific library, a stub/no-op for the training environment, or an
  external service), and what should happen to a file that fails the scan?
- **[NEEDS CLARIFICATION — Automated Testing Approach]**: No automated test project or framework
  exists anywhere in the current repository. Should this feature introduce one (and if so, what
  kind — unit, integration, or both), or should it ship without automated tests, consistent with
  the rest of the current codebase?
- **[NEEDS CLARIFICATION — Task Attachment Mechanics]**: The Stakeholder Doc requires (FR-018)
  that documents be attachable from a task detail page and auto-associated with the task's
  project, but does not specify the underlying data model or UI (e.g., whether a document can be
  attached to multiple tasks, whether this reuses `Document.ProjectId` alone or needs a dedicated
  task-document association, and what the task-detail-page upload/attach UI looks like). What is
  the required mechanism?
- **[NEEDS CLARIFICATION — Orphaned File Cleanup]** *(newly identified in final source audit,
  2026-09-09)*: TC-003's file-first upload sequence is explicitly mandated by the Stakeholder Doc
  and is retained as specified. However, the Stakeholder Doc only addresses the failure mode it
  is designed to prevent (an orphaned *database record* if the file save fails first) — it does
  not specify what should happen in the reverse case: the file is saved to disk successfully, but
  the subsequent database insert fails, leaving an orphaned *physical file* with no metadata
  record. No cleanup/rollback/retry behavior for this case is invented here. What should happen
  to the orphaned file in that scenario (e.g., delete it, leave it for a periodic cleanup job,
  retry the insert)?
- **[NEEDS CLARIFICATION — Multi-File Upload Interaction Model]** *(newly identified in final
  release-readiness review, 2026-09-09 — status: FR-001 currently PARTIAL, not satisfied)*: FR-001
  requires selecting "one or more files ... in a single action." The delivered `Documents.razor`
  upload form accepts exactly one file per `InputFile`/submission (no `multiple` attribute; a
  single `IBrowserFile` is bound), so a user must repeat the entire upload action once per file —
  "one or more files" is achievable only sequentially, not "in a single action" as FR-001 states.
  Separately, the Stakeholder Doc's own "Blazor-Specific Implementation Requirements" code example
  (§"File Upload Component State Management") is written against a single `SelectedFile` variable
  throughout, which is inconsistent with FR-001's plural, single-action requirement in the same
  document — this spec does not resolve that inconsistency by assuming either side controls. What
  is not specified by any confirmed source, and is therefore not invented here: when a user selects
  multiple files in one action, do they all share a single title/category/description/tags entry
  (one metadata submission producing N `Document` rows), or does the UI require per-file metadata
  entry? This must be answered before a multi-file upload UI is built.
- **[NEEDS CLARIFICATION — Team Lead "Team Membership" Definition]** *(newly identified in final
  release-readiness review, 2026-09-09 — status: FR-016 currently OPEN, not satisfied)*: FR-016
  requires "Team Leads can additionally view/manage documents uploaded by their team members," but
  no confirmed source defines what relationship constitutes "their team members," and the existing
  data model has no entity or field built for this purpose. `DocumentService`'s authorization logic
  (`IsAuthorizedForDocumentAsync`) implements uploader-ownership, sharing, project-membership/
  management, and Administrator access only — there is no Team-Lead-specific branch anywhere in the
  document feature's code, and no task in tasks.md ever scoped one. Candidate proxies exist in the
  data model but none is confirmed and none is assumed here: `User.Department` (free-text,
  unvalidated, organization-wide rather than team-scoped, and gated behind the still-open
  Department Claim decision above), `ProjectMember.Role` (a free-text, per-project label that
  happens to include the string `"TeamLead"` in seed data, but is not an organizational hierarchy
  and is not equivalent to project membership generally — project-level document visibility is
  already covered separately by FR-009), or an as-yet-undefined manager/reports-to relationship
  (would require a new `User` column, not present today). Project membership is explicitly NOT
  assumed to equal team membership, Department is explicitly NOT assumed to define a team, and no
  new `Team` entity is invented here. What Product/Delivery must define before this can be
  implemented: (1) which existing or new relationship authoritatively defines "team" for this
  purpose; (2) whether "view/manage" means the full uploader-equivalent capability set (view,
  download, edit metadata, replace, delete) or a subset; (3) cardinality — can a Team Lead have team
  members spanning multiple departments/projects, and can an employee have more than one Team Lead.

## Out of Scope

*(Verbatim from Stakeholder Doc §"Out of Scope")*

- Real-time collaborative editing of documents.
- Version history and rollback capabilities.
- Advanced document workflows (approval processes, document routing).
- Integration with external systems (SharePoint, OneDrive).
- Mobile app support (initial release is web-only).
- Document templates or document generation features.
- Storage quotas and quota management.
- Soft delete/trash functionality with recovery.

*(Additionally excluded per Constitution Principle II / explicit instruction for this spec)*:

- Any cloud storage implementation (e.g., Azure Blob Storage) — only the storage abstraction
  required for a future swap is in scope.
- Switching the database engine away from SQL Server LocalDB.
