# Phase 1 Contracts: Document File-Serving Endpoints

This project's only external interface for this feature is the pair of authorized HTTP endpoints
needed to serve files stored outside `wwwroot` (Constitution Complexity Tracking entry: new
`Controllers/DocumentsController`). Every other interaction (upload, browse, search, edit,
share, task/dashboard integration) happens through existing Blazor Server pages/components and is
not a separate "contract" in the API sense — it's described in `data-model.md` and will be broken
into implementation tasks in `tasks.md`.

## GET /documents/{documentId}/download

**Purpose**: Deliver the original file content for a document the requesting user is authorized
to access. (FR-011)

- **Auth**: `[Authorize]` (any authenticated role) at the controller level, **plus** an explicit
  authorization check inside `DocumentService` comparing the requesting user's id against
  document ownership / project membership / role, mirroring the existing
  `ProjectService.GetProjectByIdAsync` pattern (Constitution Principle VI). A user who is not
  authorized receives a 403/404 response (which of the two is not specified by any confirmed
  source; either satisfies "reject unauthorized access" without leaking existence — an
  implementation detail for `/speckit-tasks`, not a data-model or contract-shape concern).
- **Route parameter**: `documentId` (int).
- **Response on success**: `200 OK`, `Content-Type` set from `Document.FileType`,
  `Content-Disposition: attachment; filename="<Document.FileName>"` (the **original**, user-facing
  filename — never the physical GUID-based `FilePath` — per TC-002's "never use user-supplied
  filenames in file paths," which governs storage, not the download filename presented back to
  the user), body streamed from the file at `Document.FilePath` via `IFileStorageService.DownloadAsync`.
- **Response on failure**: `404 Not Found` if the document doesn't exist or the user isn't
  authorized (not distinguished, to avoid confirming existence to unauthorized users); `500` on
  unexpected storage errors.
- **Side effect**: MUST record a `DocumentActivityLog` entry with `ActionType = Download`
  (FR-021).

## GET /documents/{documentId}/preview

**Purpose**: Stream a document's content for in-browser preview (PDF and image types only), for
the "preview without downloading" requirement. (FR-012)

- **Auth**: identical authorization requirement as the download endpoint.
- **Route parameter**: `documentId` (int).
- **Preconditions**: `Document.FileType` must be one of the previewable types (PDF, JPEG, PNG per
  FR-002/FR-012's scope — the spec does not require preview support for Office/text files).
  Requesting preview for a non-previewable type returns `415 Unsupported Media Type` (or an
  equivalent rejection — the exact status code is an implementation detail, not specified by any
  confirmed source).
- **Response on success**: `200 OK`, `Content-Type` from `Document.FileType`, **no**
  `Content-Disposition: attachment` header (so the browser renders inline rather than downloading),
  body streamed from `IFileStorageService.DownloadAsync`.
- **Response on failure**: same as the download endpoint.
- **Side effect**: this plan does not assert whether a preview counts as a "download" for
  FR-021's activity log — the stakeholder document's "Activity Tracking" section lists "downloads"
  but does not mention "previews" separately; not resolved here to avoid inventing a requirement.

## Explicitly not contracted here

- **Upload** is not a separate HTTP endpoint contract — per the stakeholder document's
  "Blazor-Specific Implementation Requirements", upload uses the Blazor Server `InputFile`
  component and `MemoryStream` pattern in-process (`Pages/Documents.razor` calling
  `DocumentService` directly via dependency injection), not a REST endpoint.
- **Search, list, edit metadata, delete, share** are likewise in-process Blazor Server calls into
  `DocumentService`/`IProjectService`-style services, not separate HTTP contracts.
