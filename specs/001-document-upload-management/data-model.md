# Phase 1 Data Model: Document Upload and Management

All entities follow the existing `ApplicationDbContext` conventions: `int` surrogate keys,
Data Annotations for validation (`[Required]`, `[MaxLength]`), navigation properties configured in
`OnModelCreating` (relationships, indexes, seed data if any), created via `EnsureCreated()` — no
EF Core migrations are introduced (Constitution Principle VI).

## Document

Represents an uploaded file's metadata. New entity in `Models/Document.cs`.

| Field | Type | Rules | Source |
|---|---|---|---|
| `DocumentId` | `int` (PK, identity) | — | TC-005 |
| `Title` | `string` | `[Required]`, `[MaxLength(255)]` | FR-005 |
| `Description` | `string?` | `[MaxLength(2000)]`, optional | FR-005 |
| `Category` | `string` | `[Required]`, `[MaxLength(100)]`, free text (not enum) | TC-006, FR-005 |
| `FileName` | `string` | `[Required]`, `[MaxLength(255)]` — original user-supplied name, **display only, never used as a physical path** | TC-002, FR-006 |
| `FilePath` | `string` | `[Required]`, `[MaxLength(500)]` — GUID-based physical path, e.g. `{userId}/{projectId-or-"personal"}/{guid}.{ext}` | TC-002, TC-003 |
| `FileType` | `string` | `[Required]`, `[MaxLength(255)]` — MIME type | TC-007, FR-006 |
| `FileSizeBytes` | `long` | `[Required]`, must be ≤ 25 MB (enforced in `DocumentService`, not just the column) | FR-003, FR-006 |
| `UploadedByUserId` | `int` (FK → `User.UserId`) | `[Required]` | FR-006 |
| `ProjectId` | `int?` (FK → `Project.ProjectId`) | optional — null means a personal document | FR-005 |
| `Tags` | `string?` | `[MaxLength(500)]`, optional, comma-delimited or similar (exact search-tokenization approach not specified by the spec — implementation detail for `/speckit-tasks`, not a data-model concern) | FR-005, FR-010 |
| `UploadedDate` | `DateTime` | `[Required]`, set once at creation | FR-006 |
| `UpdatedDate` | `DateTime` | `[Required]`, updated on metadata edit/file replace | FR-013 |

**Relationships**: `UploadedByUser` (→ `User`, `Restrict` delete, matching existing
`TaskItem.AssignedUser`/`CreatedByUser` convention), `Project` (→ `Project`, optional, `Restrict`
delete — cascade-on-project-delete behavior is **not specified by any confirmed source** and is
not invented here).

**Indexes** (mirroring the existing `OnModelCreating` performance-index convention): `UploadedByUserId` (My Documents list, FR-008), `ProjectId` (Project Documents view, FR-009), `Category` (filter, FR-008).

## DocumentShare

Represents a sharing relationship. New entity in `Models/DocumentShare.cs`.

| Field | Type | Rules | Source |
|---|---|---|---|
| `DocumentShareId` | `int` (PK, identity) | — | FR-015 |
| `DocumentId` | `int` (FK → `Document`) | `[Required]` | FR-015 |
| `SharedWithUserId` | `int` (FK → `User`) | `[Required]` | FR-015 |
| `SharedByUserId` | `int` (FK → `User`) | `[Required]` — must equal the document's `UploadedByUserId` per FR-015 ("document owner"); enforced in `DocumentService`, not by a DB constraint | FR-015 |
| `SharedDate` | `DateTime` | `[Required]` | FR-015 |

**Relationships**: `Document` (→ `Document`, `Cascade` delete — a share record has no meaning once
its document is gone), `SharedWithUser`/`SharedByUser` (→ `User`, `Restrict` delete, matching
existing convention).

**Indexes**: unique composite index on (`DocumentId`, `SharedWithUserId`) to prevent duplicate
shares of the same document to the same user.

## DocumentActivityLog

Represents an audited action, for FR-021 (activity logging) and FR-022 (admin reporting). New
entity in `Models/DocumentActivityLog.cs`.

| Field | Type | Rules | Source |
|---|---|---|---|
| `LogId` | `int` (PK, identity) | — | FR-021 |
| `DocumentId` | `int?` (FK → `Document`) | nullable — see Relationships | FR-021 |
| `UserId` | `int` (FK → `User`) | `[Required]` — the user who performed the action | FR-021 |
| `ActionType` | `enum` (`Upload`, `Download`, `Delete`, `Share`) | `[Required]` | FR-021 |
| `Timestamp` | `DateTime` | `[Required]` | FR-021 |

**Relationships**: `Document` (→ `Document`, `SetNull` delete). Resolves the open decision
previously noted here: permanent delete (FR-014) must not be blocked by, or cascade-delete, the
audit trail (FR-021), so `DocumentId` is nullable and the log row is retained as a dangling/soft
reference (`DocumentId = NULL`) once its Document is permanently deleted, rather than being
removed or preventing the delete.

**Indexes**: (`DocumentId`, `Timestamp`), (`UserId`, `Timestamp`) — for the admin reporting
queries in FR-022 ("most active uploaders", "document access patterns").

## Modified existing entity: Notification

`Models/Notification.cs` — **no schema change**, only new `NotificationType` enum values added:
`DocumentShared`, `NewProjectDocument` (naming illustrative; exact naming is an implementation
detail, not a data-model concern). Traces to FR-020.

## Not modeled here (open decisions — see plan.md / research.md)

- **Task attachment**: no `TaskDocument` (or similar) entity is defined. The Stakeholder Doc
  requires task attachment (FR-018) but not the underlying data model; inventing a join-entity
  shape here would resolve an open decision that instruction #4 requires stay open. When the
  mechanics are decided, this file will need a corresponding update before `/speckit-tasks`
  generates data-model tasks for it.
- **Orphaned file cleanup**: no additional entity/column (e.g., a cleanup-queue table) is added
  for this, since the required behavior itself is undecided (see Open Decisions).

## Entity Relationship Summary

```
User 1---* Document (UploadedByUserId)
Project 1---* Document (ProjectId, optional)
Document 1---* DocumentShare (DocumentId)
User 1---* DocumentShare (SharedWithUserId)
User 1---* DocumentShare (SharedByUserId)
Document 1---* DocumentActivityLog (DocumentId)
User 1---* DocumentActivityLog (UserId)
```

(Existing entities `User`, `Project`, `TaskItem`, `ProjectMember`, `Notification` are otherwise
unchanged in shape, per Constitution Principle VI.)
