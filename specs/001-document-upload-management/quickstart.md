# Quickstart: Validating Document Upload and Management (US1 — MVP slice)

This guide validates the User Story 1 slice (upload a document, see it in "My Documents") end to
end once implemented. It is a **run/validation guide**, not implementation instructions — see
`data-model.md` and `contracts/document-file-endpoints.md` for design detail, and (once generated)
`tasks.md` for the build steps themselves.

## Prerequisites

- .NET 8 SDK installed (confirmed present on this machine: 8.0.425, alongside 10.0.401).
- SQL Server LocalDB installed and running.
- A clean database state, per the stakeholder document's "Database Setup Requirements": if any
  prior manual testing left orphaned records, reset LocalDB first:
  ```powershell
  sqllocaldb stop mssqllocaldb
  sqllocaldb delete mssqllocaldb
  # Recreated automatically by EnsureCreated() on next `dotnet run`
  ```

## Setup

```powershell
cd ContosoDashboard
dotnet restore
dotnet build
dotnet run
```

Open the URL shown in the terminal (e.g. `http://localhost:5000`).

## Validation scenario (mirrors spec.md User Story 1, Acceptance Scenario 1)

1. Log in as **Ni Kang** (Employee) from the login dropdown.
2. Navigate to the new **Documents** page (once implemented — see `Shared/NavMenu.razor` change
   in `plan.md`).
3. Select a PDF file under 25 MB.
4. Enter a title (e.g. "Test Document") and choose a category (e.g. "Personal Files").
5. Submit the upload.
6. **Expected**: an upload progress indicator appears; on completion, the document appears in the
   "My Documents" list with the correct title, category, upload date, file size, and uploader
   (SC-005, SC-006, FR-004, FR-005, FR-008).

## Additional validation scenarios (traced to spec.md)

- Upload a file over 25 MB → expect a clear rejection with the size limit stated (spec Acceptance
  Scenario US1-2).
- Upload an unsupported file type (e.g. `.exe`) → expect rejection naming supported types (spec
  Acceptance Scenario US1-3).
- Submit without a title or category → expect the upload to be blocked with a field-specific
  message (spec Acceptance Scenario US1-4).

## Known gaps this quickstart does NOT validate (by design — not yet decided)

- Virus-scanning behavior (Open Decision: Virus Scanning Engine).
- Any Department-claim-dependent sharing authorization (Open Decision: Department Claim).
- Task-attachment flow (Open Decision: Task Attachment Mechanics).
- Orphaned-file recovery after a simulated DB-insert failure (Open Decision: Orphaned File
  Cleanup).
- No automated test commands are listed here, since whether this feature has automated tests at
  all is itself an open decision (Open Decision: Automated Testing Approach).
