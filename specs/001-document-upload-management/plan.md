# Implementation Plan: Document Upload and Management

**Branch**: `001-document-upload-management` | **Date**: 2026-09-09 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-document-upload-management/spec.md`

## Summary

Add document upload, organization, access/management, sharing, task/dashboard integration, and
audit/reporting capabilities to the existing ContosoDashboard Blazor Server application (FR-001
through FR-022 in the spec), implemented as new entities, services, pages, and a thin controller
layer that extend the application's existing brownfield conventions rather than introducing a new
architecture. Files are stored on the local filesystem (outside `wwwroot`) behind an
`IFileStorageService` abstraction, consistent with Constitution Principle II/TC-004 and the
stakeholder document's Azure-migration design. Five items remain explicitly unresolved per the
approved spec (Department claim, virus-scanning engine, testing approach, task-attachment
mechanics, orphaned-file cleanup) and are **not** resolved by this plan.

## Technical Context

**Language/Version**: C# / .NET 8 (`net8.0`) — existing, confirmed (Constitution Principle II;
`ContosoDashboard.csproj`).

**Primary Dependencies**: ASP.NET Core 8 (Razor Pages + Blazor Server, existing), EF Core 8 /
`Microsoft.EntityFrameworkCore.SqlServer` 8.0.0 (existing). No new NuGet package is added for
storage or the database — local file I/O uses `System.IO` only, per TC-001/TC-004 and the
stakeholder document's "No Azure SDK dependencies in training implementation" note. A virus-scan
library/service is **NEEDS CLARIFICATION** — see Open Decision: Virus Scanning Engine; this plan
does not select one.

**Storage**: SQL Server LocalDB via EF Core (existing `ApplicationDbContext`, existing
`EnsureCreated()` bootstrap — no EF Core migrations are introduced, per Constitution Principle VI
and instruction #7) for metadata; local filesystem under an application-configured root directory
outside `wwwroot` (e.g. `AppData/uploads`, matching the stakeholder document's example) for
physical files, addressed via `{userId}/{projectId-or-"personal"}/{guid}.{extension}` (TC-002).

**Testing**: **NEEDS CLARIFICATION** — no test project exists anywhere in the current repository;
this plan does not decide whether to introduce one. See Open Decision: Automated Testing Approach.

**Target Platform**: Existing ASP.NET Core Blazor Server web app, run locally via
`dotnet run` against SQL Server LocalDB (unchanged).

**Project Type**: Single existing web application project (`ContosoDashboard/`) — this feature
extends it in place; no new project, solution, or service is created.

**Performance Goals**: Upload completes within 30s for files up to 25MB (SC-006); document list of
up to 500 items loads within 2s (SC-007); search returns within 2s (SC-008); preview loads within
3s (SC-009) — all from the approved spec, technology-agnostic there and unchanged here.

**Constraints**: Offline-capable, no cloud dependency (Constitution Principle II); files stored
outside `wwwroot` (TC-001); GUID-based physical filenames, never user-supplied names (TC-002);
25MB per-file limit, PDF/Office/text/JPEG/PNG only (FR-002, FR-003); integer `DocumentId` (TC-005);
free-text `Category` (TC-006); `FileType` column sized for 255 characters (TC-007); upload sequence
must be generate-path → save-file → save-DB-record, exactly as the stakeholder document specifies
(TC-003) — the reverse-failure orphaned-file case remains an open decision, not resolved here.

**Scale/Scope**: Single ContosoDashboard deployment; stakeholder document's target population is
all Contoso employees (~5,000) with role-based access across 4 roles (Employee, TeamLead,
ProjectManager, Administrator) — unchanged from the existing application's user base.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design (see "Post-Design
Re-check" below).*

Checked against `.specify/memory/constitution.md` v1.1.0:

- **Principle I (Spec-Driven Development)**: PASS. This plan follows an approved spec; no
  application code is written by this command; `/speckit-tasks` and implementation are deferred.
- **Principle II (Confirmed Technology Stack Only)**: PASS. .NET 8, SQL Server LocalDB, EF Core,
  mock cookie auth, and local-filesystem-with-`IFileStorageService`-abstraction are all preserved
  exactly as confirmed; no substitution (no SQLite, no other DB engine, no cloud storage
  implementation) is introduced.
- **Principle III (No Invented Requirements)**: PASS. Every technical decision in this plan traces
  to the approved spec, the stakeholder document, or verified existing code; where the spec left an
  item open, this plan leaves it open too rather than inventing a resolution (see below).
- **Principle IV (Explicit Decision Logging)**: PASS, with one flagged exception requiring your
  attention: introducing an MVC controller for authorized file download/preview is a **new**
  pattern for this codebase (no `Controllers/` folder exists today). This is not a default chosen
  for convenience — it is directly required by the stakeholder document's own security section
  ("Files stored outside `wwwroot` require controller endpoints to serve them (enables
  authorization checks)"), so it is treated as a source-required, justified deviation rather than
  an assumed default. Logged formally in **Complexity Tracking** below rather than silently added.
- **Principle V (Human-Owned Version Control)**: PASS. No commits made by this command.
- **Principle VI (Preserve Existing Brownfield Conventions)**: PASS, by design — see Project
  Structure below: interface+implementation co-located per service file, `AddScoped` DI with
  direct `ApplicationDbContext` injection, service-layer `requestingUserId` IDOR checks, Blazor
  pages reading `ClaimTypes.NameIdentifier` via `AuthenticationStateProvider`, EF Core
  configuration centralized in `OnModelCreating`, `int` surrogate keys, `EnsureCreated()` (no
  migrations) — all retained for the new document feature. The one addition outside these existing
  patterns (the file-serving controller) is logged in Complexity Tracking, not silently introduced.

**Result: PASS**, with the one logged, source-justified deviation (file-serving controller).

## Project Structure

### Documentation (this feature)

```text
specs/001-document-upload-management/
├── spec.md               # Approved feature specification
├── plan.md               # This file (/speckit-plan command output)
├── research.md           # Phase 0 output — includes 5 explicitly DEFERRED decisions
├── data-model.md         # Phase 1 output
├── quickstart.md         # Phase 1 output
├── contracts/            # Phase 1 output — file-serving endpoint contracts
│   └── document-file-endpoints.md
├── checklists/
│   └── requirements.md   # From /speckit-specify
└── tasks.md              # Phase 2 output (/speckit-tasks — NOT created by this command)
```

### Source Code (repository root)

This feature extends the **existing single ASP.NET Core Blazor Server project**
(`ContosoDashboard/`) in place. No new project, solution, `src/`, `backend/`, or `frontend/`
split is introduced — Options 1-3 in the template do not apply; the existing single-project
structure is used as-is, per Constitution Principle VI.

```text
ContosoDashboard/
├── Models/
│   ├── Document.cs                       # NEW — Document entity (TC-005, TC-006, TC-007)
│   ├── DocumentShare.cs                  # NEW — sharing relationship entity
│   ├── DocumentActivityLog.cs            # NEW — audit log entity (FR-021, FR-022)
│   └── Notification.cs                   # MODIFY — add document-related NotificationType values
├── Data/
│   └── ApplicationDbContext.cs           # MODIFY — add DbSets, relationships, indexes,
│                                          #          OnModelCreating entries for the 3 new
│                                          #          entities (no migrations; EnsureCreated stays)
├── Services/
│   ├── LocalFileStorageService.cs        # NEW — IFileStorageService + LocalFileStorageService
│   │                                      #        (TC-004), file named after the concrete class
│   │                                      #        per existing convention
│   └── DocumentService.cs                # NEW — IDocumentService + DocumentService: validation,
│                                          #        requestingUserId-based authorization,
│                                          #        orchestrates storage + DB per TC-003
├── Controllers/                          # NEW folder — see Complexity Tracking
│   └── DocumentsController.cs            # NEW — authorized download/preview streaming endpoints
│                                          #        (stakeholder-mandated; see contracts/)
├── Pages/
│   ├── Documents.razor                   # NEW — "My Documents" view: upload, list, sort,
│   │                                      #        filter, search, edit metadata, delete, share
│   ├── Index.razor                       # MODIFY — add "Recent Documents" widget + doc count
│   ├── ProjectDetails.razor              # MODIFY — add project documents list/upload
│   ├── Tasks.razor                       # MODIFY — task attachment UI (mechanics open; see
│   │                                      #          Open Decisions: Task Attachment)
│   └── Login.cshtml.cs                   # NOT MODIFIED by this plan — Department claim addition
│                                          #        is an open decision, not assumed in scope
├── Shared/
│   └── NavMenu.razor                     # MODIFY — add "Documents" navigation link
├── Program.cs                            # MODIFY — register IFileStorageService/
│                                          #          LocalFileStorageService and IDocumentService/
│                                          #          DocumentService as Scoped (existing DI
│                                          #          convention); map controller routes
└── appsettings.json                      # MODIFY — add configurable local storage root path
                                           #          (e.g. "DocumentStorage:RootPath")
```

**Structure Decision**: Single existing project, extended in place. New entities/services/pages
are added following the file-per-service (interface+impl colocated) and Scoped-DI conventions
already used by `ProjectService.cs`, `NotificationService.cs`, etc. The one structural addition —
a `Controllers/` folder — is new to this codebase but is directly required by the stakeholder
document (see Complexity Tracking) and is scoped to exactly the two authorized file-serving
endpoints; it does not introduce a general MVC API layer alongside Blazor Server.

## Complexity Tracking

> Filled because Constitution Check identified one deviation from existing (zero) precedent that
> requires justification, per Principle VI's own exception clause (deviation is permitted when a
> confirmed requirement demonstrably requires it).

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|---------------------------------------|
| Introducing an MVC `Controllers/` folder and `DocumentsController` (no API controller exists in this codebase today; all existing pages are Razor Pages or Blazor components) | The stakeholder document explicitly requires files to be stored outside `wwwroot`, and states "Files stored outside `wwwroot` require controller endpoints to serve them (enables authorization checks)" (§"Security Considerations"). Blazor Server components cannot themselves stream an authorized file response with proper `Content-Disposition`/content-type headers back to the browser. | Serving files directly from `wwwroot` was rejected because it is explicitly prohibited by TC-001 and would bypass the service-layer IDOR authorization check required by FR-017/TC-008 entirely — any authenticated (or even unauthenticated, depending on static file config) request could fetch any file by guessing/enumerating its static path. |

No other Constitution Check items required an entry — every other new component follows an
existing, established convention (see Project Structure above).

---

## Phase 0: Research

See [research.md](./research.md) for the full output. Summary: most technology decisions were
already fixed by the approved spec and constitution (not open choices), so Phase 0 here mainly
confirms alignment rather than exploring alternatives. The five items the spec left open are
**not** resolved here — each is recorded in research.md as `Decision: DEFERRED`, consistent with
instruction #4 (do not silently resolve any [NEEDS CLARIFICATION] item) and Constitution
Principle IV.

## Phase 1: Design & Contracts

See [data-model.md](./data-model.md), [contracts/document-file-endpoints.md](./contracts/document-file-endpoints.md),
and [quickstart.md](./quickstart.md) for full output.

### Post-Design Re-check (Constitution Check, re-evaluated after Phase 1)

- Data model (data-model.md) uses `int` keys throughout, free-text `Category`, a 255-character
  `FileType` column, and centralizes configuration in `ApplicationDbContext.OnModelCreating` —
  consistent with Principle VI and TC-005/TC-006/TC-007. **PASS.**
- The two file-serving contracts require `[Authorize]` plus an explicit
  `requestingUserId`/ownership-or-membership check inside `DocumentService`, mirroring
  `ProjectService.GetProjectByIdAsync`'s pattern exactly (return "not found"/403 rather than
  leaking existence to unauthorized users) — consistent with Principle VI's IDOR-checking
  convention. **PASS.**
- No new database engine, ORM, or repository/unit-of-work layer was introduced during design.
  **PASS.**
- Re-affirms: the five open decisions remain open; none was resolved during Phase 1 design.
  **PASS** (nothing in Phase 1 design required resolving them to proceed — the schema and
  contracts are correct either way each open decision is eventually resolved).

**Result: PASS.** No new Complexity Tracking entries from Phase 1.
