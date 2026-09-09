<!--
Sync Impact Report
- Version change: 1.0.0 → 1.1.0
- Modified principles:
  - II. Confirmed Technology Stack Only — expanded with concrete facts verified
    against the now-imported real codebase and installed SDKs (was written
    blind, before any app code or stakeholder doc existed).
  - III. No Invented Requirements — rationale updated: a real stakeholder
    requirements document now exists
    (StakeholderDocs/document-upload-and-management-feature.md) and is the
    confirmed source for the 001-document-upload-management feature.
- Added principles:
  - VI. Preserve Existing Brownfield Conventions (NEW) — codifies the
    architectural/code conventions actually observed in the imported
    ContosoDashboard codebase, so new feature work extends them rather than
    introducing parallel patterns.
- Added sections: none (existing section structure kept; content expanded)
- Removed sections: none
- Templates requiring follow-up review (not modified by this command):
  - .specify/templates/spec-template.md — confirm it still separates
    confirmed requirements / assumptions / open questions per Principle III
    now that a real stakeholder doc exists.
  - .specify/templates/plan-template.md — confirm it references the
    brownfield conventions in Principle VI rather than defaulting to a
    generic layered architecture.
- Deferred/TODO items: none. Last Amended date is the date of this review.
-->

# ContosoDashboard Constitution

## Core Principles

### I. Spec-Driven Development Workflow (NON-NEGOTIABLE)
Every feature MUST proceed through the Spec Kit stage sequence in order —
constitution → specify → plan → tasks → implement — using the installed
Spec Kit tooling (`/speckit-*` skills) as the mechanism for each stage.
No implementation code, application scaffolding, or project structure may be
created before a specification exists for the feature, and no specification
stage may be skipped. Each stage's output (this constitution, a feature spec,
an implementation plan, a task list) MUST be presented for explicit human
approval before the next stage begins.
Rationale: this project was explicitly commissioned to be built via
Spec-Driven Development; skipping or reordering stages defeats the reason
Spec Kit was adopted.

### II. Confirmed Technology Stack Only
The following technology choices are fixed project requirements, verified
against the actual imported codebase and this machine's installed SDKs, and
MUST NOT be substituted, downgraded, or worked around without an explicit,
recorded decision from the project owner:
- **.NET 8** (`net8.0`) as the application runtime/framework target —
  confirmed in `ContosoDashboard.csproj`; the .NET 8 SDK (8.0.425) is
  installed alongside .NET 10, so no framework-version workaround is needed.
- **ASP.NET Core 8 with Blazor Server** (Razor Pages for `/login` and
  `/logout`, Blazor components for everything else) — confirmed in
  `Program.cs` (`AddRazorPages`, `AddServerSideBlazor`, `MapBlazorHub`,
  `MapFallbackToPage("/_Host")`).
- **SQL Server LocalDB** via `Microsoft.EntityFrameworkCore.SqlServer`
  8.0.0, confirmed in `appsettings.json`
  (`Server=(localdb)\mssqllocaldb;Database=ContosoDashboard;...`) and
  `ContosoDashboard.csproj`. Substituting a different engine (including
  SQLite, in-memory providers, or containerized SQL Server) for convenience
  is explicitly prohibited unless the project owner records a new decision
  amending this principle.
- **Mock cookie-based authentication for training**, confirmed in
  `Program.cs` (`AddAuthentication(CookieAuthenticationDefaults...)
  .AddCookie(...)`) and `Pages/Login.cshtml.cs`. The `Microsoft.Identity.Web`
  packages and the `AzureAd` section in `appsettings.json` are present but
  **unused** — real Entra ID sign-in is a future production migration path,
  not the current implementation. The current login flow issues claims
  `NameIdentifier`, `Name`, `Email`, `Role` — it does **not** currently issue
  a `Department` claim, even though `User.Department` exists on the data
  model and is used by `IUserService.GetTeamMembersAsync`. Any feature that
  needs `Department` in the authentication claims must treat adding it as an
  explicit, planned change to the login flow, not an assumption.
- **GitHub Spec Kit** as the Spec-Driven Development tooling for this
  repository, with a Claude Code integration (no GitHub Copilot integration
  files are maintained in this repository).
- For the document upload and management feature specifically, the
  stakeholder requirements document additionally confirms: local filesystem
  storage (not cloud) for the training implementation, an `IFileStorageService`
  interface abstraction (not yet present in the codebase) so a future
  `AzureBlobStorageService` can be swapped in without changing business
  logic, integer (not GUID) primary keys for new entities for consistency
  with existing `User`/`Project`/`TaskItem` keys, and free-text (not enum)
  category values.
- Version control: Git, hosted on GitHub. `main` MUST NOT be modified
  directly for feature work; feature work happens on dedicated feature
  branches (e.g. `001-document-upload-management`).
Rationale: these are the technology commitments that are actually verifiable
today — either in the imported source code, in this machine's installed
tooling, or in the stakeholder requirements document. Nothing else about the
stack is confirmed and MUST NOT be assumed under this principle.

### III. No Invented Requirements
Functional requirements, business rules, user stories, and acceptance
criteria MUST trace to a confirmed source: `StakeholderDocs/document-upload-
and-management-feature.md` (the confirmed stakeholder input for the
001-document-upload-management feature), other repository documentation, an
explicit statement from the project owner, or a decision recorded as an
amendment to project artifacts. Where a plausible-sounding requirement is
not confirmed by one of these sources, it MUST be written down as an
**assumption** or an **open question**, not implemented as if settled.
Specifications and plans MUST visibly separate confirmed requirements from
assumptions and from open questions/decisions required.
Rationale: a real, detailed stakeholder requirements document now exists;
treating anything beyond it (or beyond the imported codebase) as a
requirement would silently commit the project owner to decisions they never
made.

### IV. Explicit Decision Logging for Unconfirmed Defaults
Where an implementation choice is not dictated by Principle II, Principle VI,
or by a confirmed requirement under Principle III — including but not
limited to authentication/authorization mechanisms beyond what already
exists, specific libraries, naming conventions not already established in
the codebase, or UI/UX details not specified by stakeholders — it MUST be
flagged as an open decision in the relevant spec or plan artifact rather
than silently chosen because it is a common or conventional default.
Common-default reasoning ("most projects do X") is not by itself sufficient
justification to treat X as settled.
Rationale: the project owner has repeatedly required that defaults be
surfaced as decisions rather than assumed, to keep control over choices that
are common but not actually confirmed.

### V. Human-Owned Version Control & No AI Attribution
Git authorship, commit messages, pull requests, and repository metadata MUST
use only the project's authorized human/team Git identity. Commits, PRs, and
tracked repository files MUST NOT include AI-tool attribution, AI co-author
trailers, AI session links or transcripts, or AI-tool-specific configuration
unless the project genuinely requires such config to function. Local
AI-assistant tooling artifacts (e.g. agent-local skill or command files, such
as `.claude/`) MAY exist on disk for development convenience but MUST be
excluded from version control (via `.gitignore`) rather than committed. The
human project owner retains sole control over what is staged, committed, and
pushed; an AI assistant may prepare changes but does not commit or push on
its own authority.
Rationale: explicitly required by the project owner as a standing rule for
this repository's history and metadata.

### VI. Preserve Existing Brownfield Conventions
ContosoDashboard is a brownfield project with real, working conventions
already established across the imported codebase. New feature work
(including document upload and management) MUST follow these conventions
unless a confirmed requirement demonstrably requires deviation, and MUST NOT
introduce a parallel or "improved" pattern merely because it is considered
a common best practice elsewhere. Observed conventions that MUST be
preserved:
- Each service defines its interface and implementation **in the same file**
  (e.g. `IProjectService`/`ProjectService` both in `ProjectService.cs`), is
  registered with `AddScoped<TInterface, TImplementation>` in `Program.cs`,
  and takes `ApplicationDbContext` directly via constructor injection — there
  is no repository or unit-of-work layer between services and EF Core.
- Authorization/IDOR protection is enforced **inside service methods**, by
  comparing a passed-in `requestingUserId` against the resource's owning
  user/project-membership, returning `null`/`false` on failure — not solely
  by page-level `[Authorize]` role gating. New document-related services
  (e.g. a `DocumentService`) MUST follow this same explicit-check pattern.
- Blazor pages are annotated `@attribute [Authorize]`, `@inject` the
  services they need, and read the current user's id from
  `ClaimTypes.NameIdentifier` via `AuthenticationStateProvider` in
  `OnInitializedAsync` — not from a different identity mechanism.
- All EF Core relationships, indexes, and seed data are configured
  centrally in `ApplicationDbContext.OnModelCreating`, not scattered across
  migrations-first configuration or separate `IEntityTypeConfiguration`
  classes.
- Primary keys across existing entities (`User`, `Project`, `TaskItem`,
  `Notification`, `ProjectMember`, `Announcement`, `TaskComment`) are `int`
  surrogate keys, not GUIDs — new entities must match.
- The database is created via `context.Database.EnsureCreated()` at startup
  (no EF Core migrations are currently in use); this MUST NOT be silently
  replaced with a migrations-based workflow without an explicit decision.
Rationale: the project owner has explicitly required that existing working
abstractions not be replaced without a demonstrated requirement, and that no
new architecture be introduced merely because it's a default best practice.
Documenting the actual conventions here is what makes that rule enforceable
during spec, plan, and implementation review.

## Technology & Environment Constraints

- Runtime/framework: .NET 8 (`net8.0`), ASP.NET Core + Blazor Server
  (confirmed; see Principle II).
- Data store: SQL Server LocalDB via EF Core 8 (confirmed; see Principle
  II). No alternative database engine is authorized without an explicit
  amendment.
- Authentication: mock cookie-based auth for training, claims currently
  limited to `NameIdentifier`/`Name`/`Email`/`Role` (confirmed; see
  Principle II). `Microsoft.Identity.Web`/Entra ID packages are present in
  the project file but inactive.
- File storage for the document feature: local filesystem (offline-first,
  per the stakeholder requirements document), behind an `IFileStorageService`
  abstraction to be introduced, enabling a future Azure Blob Storage
  implementation without business-logic changes. This does not exist in the
  codebase yet — it is a confirmed requirement to be designed, not an
  existing convention.
- Spec-Driven Development tooling: GitHub Spec Kit, initialized in this
  repository with the `claude` agent integration and PowerShell (`ps`)
  helper scripts.
- Version control: Git, hosted on GitHub. `main` is the stable branch and
  MUST NOT be modified directly for feature work; feature work happens on
  dedicated feature branches (e.g. `001-document-upload-management`)
  following Spec Kit's numbered feature-branch convention.
- No automated test project or test framework currently exists in the
  repository (confirmed by inspection — no `*Tests*` project or files were
  found). Whether to introduce one for the document feature is an open
  decision under Principle IV, not something to assume either way.
- Everything else about the technical environment not stated above or in
  Principle VI (e.g. CI/CD, hosting/deployment target, logging/observability
  beyond the default ASP.NET Core logger) remains **not yet confirmed** and
  MUST be treated as an open decision under Principle IV when first
  encountered in a spec or plan.

## Development Workflow & Stage Gates

- Stage order is fixed: constitution → specify → plan → tasks → implement,
  per Principle I. Optional Spec Kit enhancement stages (`/speckit-clarify`,
  `/speckit-analyze`, `/speckit-checklist`) may be used between the fixed
  stages but do not replace them.
- Before executing each major stage (creating/updating the constitution, the
  feature specification, the implementation plan, the task list, or writing
  application code), the following MUST be presented to the project owner
  for approval first: what will be created or changed, why it is required,
  any assumptions being made, and any open decisions that need a call from
  the project owner.
- New application code for the document upload and management feature MUST
  conform to Principle VI's observed conventions unless the approved spec or
  plan explicitly documents and justifies a deviation.
- No further `.NET` source code may be added or modified until both the
  feature specification and the implementation plan for that feature have
  been reviewed and approved.
- Commits and pushes are performed only with the project owner's direction;
  the working tree and `git status`/`git diff` MUST be shown before
  committing.

## Governance

This constitution supersedes ad hoc practice for this repository. Amendments
are made by editing `.specify/memory/constitution.md`, incrementing the
version per semantic versioning (MAJOR: incompatible principle removal or
redefinition; MINOR: new principle or materially expanded guidance; PATCH:
clarification/wording), and prepending an updated Sync Impact Report. Every
feature specification and implementation plan MUST be checked for compliance
with these principles before its stage gate is considered passed; any
conflict between a proposed spec/plan and this constitution MUST be resolved
by amending one of them explicitly, never by silent deviation.

**Version**: 1.1.0 | **Ratified**: 2026-09-09 | **Last Amended**: 2026-09-09
