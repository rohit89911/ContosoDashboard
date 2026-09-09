using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IDocumentService
{
    Task<Document> UploadAsync(Stream fileStream, string fileName, long fileSizeBytes, string contentType,
        string title, string? description, string category, int? projectId, string? tags, int requestingUserId);
    Task<List<Document>> GetMyDocumentsAsync(int userId, string? sortBy = null, string? category = null,
        int? projectId = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<Document>> GetProjectDocumentsAsync(int projectId, int requestingUserId);
    Task<List<Document>> SearchDocumentsAsync(string query, int requestingUserId);
    Task<Document?> GetDocumentForAccessAsync(int documentId, int requestingUserId);
    Task<bool> UpdateMetadataAsync(int documentId, string title, string? description, string category,
        string? tags, int requestingUserId);
    Task<bool> ReplaceFileAsync(int documentId, Stream fileStream, string fileName, long fileSizeBytes,
        string contentType, int requestingUserId);
    Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId);
    Task<bool> ShareDocumentAsync(int documentId, int sharedWithUserId, int requestingUserId);
    Task<List<Document>> GetSharedWithMeAsync(int userId);
    Task<List<Document>> GetRecentDocumentsAsync(int userId, int count = 5);
    Task LogActivityAsync(int documentId, int userId, DocumentActivityType actionType);
    Task<DocumentActivityReport> GetDocumentActivityReportAsync();
}

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly INotificationService _notificationService;

    private const long MaxFileSizeBytes = 25L * 1024 * 1024; // FR-003: 25 MB

    public DocumentService(ApplicationDbContext context, IFileStorageService fileStorageService,
        INotificationService notificationService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _notificationService = notificationService;
    }

    // T011 [US1]: upload validation + TC-003 sequence (generate path -> save file -> save DB
    // record) + T035 [US6]: notify project members on a new project document.
    public async Task<Document> UploadAsync(Stream fileStream, string fileName, long fileSizeBytes,
        string contentType, string title, string? description, string category, int? projectId,
        string? tags, int requestingUserId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category is required.", nameof(category));
        if (fileSizeBytes > MaxFileSizeBytes)
            throw new InvalidOperationException("File exceeds the 25 MB limit.");

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !DocumentFileTypes.IsAllowedExtension(extension))
            throw new InvalidOperationException("Unsupported file type.");

        // OPEN DECISION: Virus Scanning Engine/Library (unresolved — see spec.md/tasks.md).
        // No scanning engine is selected or invoked here. This call site is an explicit,
        // clearly-marked extension point for FR-007, intentionally left as a no-op stub so the
        // rest of the upload pipeline remains independently testable without inventing a
        // scanning technology.
        _ = await ScanForVirusesAsync(fileStream);

        // TC-003, verified against the stakeholder document (file-first):
        // Step 1 - generate the unique path.
        var relativePath = GenerateRelativeStoragePath(requestingUserId, projectId, extension);

        // Step 2 - save the file to disk.
        await _fileStorageService.UploadAsync(fileStream, relativePath, contentType);

        // Step 3 - save the metadata record to the database.
        // OPEN DECISION: Orphaned File Cleanup (unresolved — see spec.md/tasks.md/plan.md).
        // If SaveChangesAsync below throws, the file saved in Step 2 becomes orphaned (no DB
        // record references it). No cleanup/rollback/retry/compensation behavior is implemented
        // for that case — this is a deliberate BLOCKED placeholder, not a chosen "do nothing"
        // strategy: the exception is simply allowed to propagate to the caller unhandled here.
        var document = new Document
        {
            Title = title,
            Description = description,
            Category = category,
            FileName = fileName,
            FilePath = relativePath,
            FileType = contentType,
            FileSizeBytes = fileSizeBytes,
            UploadedByUserId = requestingUserId,
            ProjectId = projectId,
            Tags = tags,
            UploadedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        await LogActivityAsync(document.DocumentId, requestingUserId, DocumentActivityType.Upload);

        // T035 [US6]: notify project members when a document is added to their project.
        if (projectId.HasValue)
        {
            await NotifyProjectMembersOfNewDocumentAsync(projectId.Value, document, requestingUserId);
        }

        return document;
    }

    // T012 [US1] / T016 [US2]: "My Documents" list with sort/filter.
    public async Task<List<Document>> GetMyDocumentsAsync(int userId, string? sortBy = null,
        string? category = null, int? projectId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.Documents
            .Include(d => d.UploadedByUser)
            .Include(d => d.Project)
            .Where(d => d.UploadedByUserId == userId);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(d => d.Category == category);

        if (projectId.HasValue)
            query = query.Where(d => d.ProjectId == projectId.Value);

        if (fromDate.HasValue)
            query = query.Where(d => d.UploadedDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(d => d.UploadedDate <= toDate.Value);

        query = sortBy switch
        {
            "title" => query.OrderBy(d => d.Title),
            "category" => query.OrderBy(d => d.Category),
            "fileSize" => query.OrderByDescending(d => d.FileSizeBytes),
            _ => query.OrderByDescending(d => d.UploadedDate)
        };

        return await query.ToListAsync();
    }

    // T017 [US2]: project documents, authorized via the same membership/manager pattern as
    // ProjectService.GetProjectByIdAsync.
    public async Task<List<Document>> GetProjectDocumentsAsync(int projectId, int requestingUserId)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectMembers)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

        if (project == null) return new List<Document>();

        var isProjectManager = project.ProjectManagerId == requestingUserId;
        var isProjectMember = project.ProjectMembers.Any(pm => pm.UserId == requestingUserId);

        if (!isProjectManager && !isProjectMember)
        {
            return new List<Document>(); // User not authorized
        }

        return await _context.Documents
            .Include(d => d.UploadedByUser)
            .Where(d => d.ProjectId == projectId)
            .OrderByDescending(d => d.UploadedDate)
            .ToListAsync();
    }

    // T019 [US2]: search scoped to documents the requesting user is authorized to see.
    public async Task<List<Document>> SearchDocumentsAsync(string query, int requestingUserId)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<Document>();

        var isAdministrator = await IsAdministratorAsync(requestingUserId);

        var memberProjectIds = await _context.ProjectMembers
            .Where(pm => pm.UserId == requestingUserId)
            .Select(pm => pm.ProjectId)
            .ToListAsync();
        var managedProjectIds = await _context.Projects
            .Where(p => p.ProjectManagerId == requestingUserId)
            .Select(p => p.ProjectId)
            .ToListAsync();
        var accessibleProjectIds = memberProjectIds.Union(managedProjectIds).ToHashSet();

        var lowered = query.ToLower();

        var candidates = _context.Documents
            .Include(d => d.UploadedByUser)
            .Include(d => d.Project)
            .Where(d =>
                d.Title.ToLower().Contains(lowered) ||
                (d.Description != null && d.Description.ToLower().Contains(lowered)) ||
                (d.Tags != null && d.Tags.ToLower().Contains(lowered)) ||
                d.UploadedByUser.DisplayName.ToLower().Contains(lowered) ||
                (d.Project != null && d.Project.Name.ToLower().Contains(lowered)));

        if (!isAdministrator)
        {
            candidates = candidates.Where(d =>
                d.UploadedByUserId == requestingUserId ||
                (d.ProjectId != null && accessibleProjectIds.Contains(d.ProjectId.Value)));
        }

        return await candidates.OrderByDescending(d => d.UploadedDate).ToListAsync();
    }

    // Authorization-checked fetch used by DocumentsController for download (T022) and preview
    // (T023) — mirrors ProjectService.GetProjectByIdAsync's "return null if unauthorized" style.
    public async Task<Document?> GetDocumentForAccessAsync(int documentId, int requestingUserId)
    {
        var document = await _context.Documents
            .Include(d => d.Project)
            .ThenInclude(p => p!.ProjectMembers)
            .Include(d => d.Shares)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);

        if (document == null) return null;

        if (await IsAuthorizedForDocumentAsync(document, requestingUserId))
        {
            return document;
        }

        return null;
    }

    // T025 [US4]: only the uploader may edit metadata (FR-013, literal scope — not extended to
    // Project Managers, since FR-013 names only "the uploader").
    public async Task<bool> UpdateMetadataAsync(int documentId, string title, string? description,
        string category, string? tags, int requestingUserId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null) return false;
        if (document.UploadedByUserId != requestingUserId) return false;

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(category))
            return false;

        document.Title = title;
        document.Description = description;
        document.Category = category;
        document.Tags = tags;
        document.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    // T026 [US4]: replace the file, uploader-only, same TC-003 file-first ordering applied to
    // the new file; the OLD file is removed only after the DB update succeeds.
    public async Task<bool> ReplaceFileAsync(int documentId, Stream fileStream, string fileName,
        long fileSizeBytes, string contentType, int requestingUserId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null) return false;
        if (document.UploadedByUserId != requestingUserId) return false;

        if (fileSizeBytes > MaxFileSizeBytes)
            throw new InvalidOperationException("File exceeds the 25 MB limit.");

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !DocumentFileTypes.IsAllowedExtension(extension))
            throw new InvalidOperationException("Unsupported file type.");

        var oldRelativePath = document.FilePath;

        // Generate new path -> save new file -> update DB record (same TC-003 ordering).
        // OPEN DECISION: Orphaned File Cleanup applies identically here if the update below
        // fails after the new file is saved — not resolved, no behavior invented (see UploadAsync
        // for the identical note).
        var newRelativePath = GenerateRelativeStoragePath(requestingUserId, document.ProjectId, extension);
        await _fileStorageService.UploadAsync(fileStream, newRelativePath, contentType);

        document.FileName = fileName;
        document.FilePath = newRelativePath;
        document.FileType = contentType;
        document.FileSizeBytes = fileSizeBytes;
        document.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Only remove the old physical file after the DB update has committed successfully.
        await _fileStorageService.DeleteAsync(oldRelativePath);

        return true;
    }

    // T027 [US4]: permanent delete ONLY (FR-014). No soft-delete/trash/recovery is implemented —
    // that is explicitly out of scope per the stakeholder document.
    public async Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId)
    {
        var document = await _context.Documents
            .Include(d => d.Project)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);

        if (document == null) return false;

        var isUploader = document.UploadedByUserId == requestingUserId;
        var isProjectManager = document.Project != null && document.Project.ProjectManagerId == requestingUserId;

        if (!isUploader && !isProjectManager) return false;

        // Record the deletion in the activity log before removing the document itself, so the
        // Delete action is captured while DocumentId still points at a live Document row.
        await LogActivityAsync(document.DocumentId, requestingUserId, DocumentActivityType.Delete);

        // DocumentActivityLog.DocumentId is nullable with DeleteBehavior.SetNull (see
        // ApplicationDbContext), so removing the Document here nulls out DocumentId on this and
        // every other log row for it instead of deleting them or being blocked by them — the
        // audit trail (FR-021), including the Delete record just written above, survives.
        var relativePath = document.FilePath;

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        await _fileStorageService.DeleteAsync(relativePath);

        return true;
    }

    // T030 [US5]: share, owner-only (FR-015).
    public async Task<bool> ShareDocumentAsync(int documentId, int sharedWithUserId, int requestingUserId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null) return false;
        if (document.UploadedByUserId != requestingUserId) return false;

        var alreadyShared = await _context.DocumentShares
            .AnyAsync(ds => ds.DocumentId == documentId && ds.SharedWithUserId == sharedWithUserId);
        if (alreadyShared) return false;

        _context.DocumentShares.Add(new DocumentShare
        {
            DocumentId = documentId,
            SharedWithUserId = sharedWithUserId,
            SharedByUserId = requestingUserId,
            SharedDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await LogActivityAsync(documentId, requestingUserId, DocumentActivityType.Share);

        await _notificationService.CreateNotificationAsync(new Notification
        {
            UserId = sharedWithUserId,
            Title = "A document was shared with you",
            Message = $"\"{document.Title}\" was shared with you.",
            Type = NotificationType.DocumentShared,
            Priority = NotificationPriority.Informational
        });

        return true;
    }

    // T031 [US5]: "Shared with Me".
    public async Task<List<Document>> GetSharedWithMeAsync(int userId)
    {
        return await _context.DocumentShares
            .Include(ds => ds.Document)
            .ThenInclude(d => d.UploadedByUser)
            .Where(ds => ds.SharedWithUserId == userId)
            .OrderByDescending(ds => ds.SharedDate)
            .Select(ds => ds.Document)
            .ToListAsync();
    }

    // T033 [US6]: dashboard "Recent Documents" widget.
    public async Task<List<Document>> GetRecentDocumentsAsync(int userId, int count = 5)
    {
        return await _context.Documents
            .Where(d => d.UploadedByUserId == userId)
            .OrderByDescending(d => d.UploadedDate)
            .Take(count)
            .ToListAsync();
    }

    // FR-021: used by UploadAsync (T011), DeleteDocumentAsync (T027), ShareDocumentAsync (T030),
    // and DocumentsController's download/preview actions (T022/T023).
    public async Task LogActivityAsync(int documentId, int userId, DocumentActivityType actionType)
    {
        _context.DocumentActivityLogs.Add(new DocumentActivityLog
        {
            DocumentId = documentId,
            UserId = userId,
            ActionType = actionType,
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    // T037 [US7]: admin reporting, backed entirely by the log rows written above.
    public async Task<DocumentActivityReport> GetDocumentActivityReportAsync()
    {
        var mostUploadedTypes = await _context.Documents
            .GroupBy(d => d.FileType)
            .Select(g => new FileTypeCount { FileType = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        var mostActiveUploaders = await _context.DocumentActivityLogs
            .Where(l => l.ActionType == DocumentActivityType.Upload)
            .Include(l => l.User)
            .GroupBy(l => new { l.UserId, l.User.DisplayName })
            .Select(g => new UploaderCount { UserId = g.Key.UserId, DisplayName = g.Key.DisplayName, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        var accessPatterns = await _context.DocumentActivityLogs
            .GroupBy(l => l.ActionType)
            .Select(g => new ActionTypeCount { ActionType = g.Key, Count = g.Count() })
            .ToListAsync();

        return new DocumentActivityReport
        {
            MostUploadedTypes = mostUploadedTypes,
            MostActiveUploaders = mostActiveUploaders,
            AccessPatterns = accessPatterns
        };
    }

    private async Task<bool> IsAuthorizedForDocumentAsync(Document document, int requestingUserId)
    {
        if (document.UploadedByUserId == requestingUserId) return true;

        if (document.Shares.Any(s => s.SharedWithUserId == requestingUserId)) return true;

        if (document.Project != null)
        {
            var isProjectManager = document.Project.ProjectManagerId == requestingUserId;
            var isProjectMember = document.Project.ProjectMembers.Any(pm => pm.UserId == requestingUserId);
            if (isProjectManager || isProjectMember) return true;
        }

        return await IsAdministratorAsync(requestingUserId);
    }

    private async Task<bool> IsAdministratorAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.Role == UserRole.Administrator;
    }

    private async Task NotifyProjectMembersOfNewDocumentAsync(int projectId, Document document, int uploaderId)
    {
        var memberUserIds = await _context.ProjectMembers
            .Where(pm => pm.ProjectId == projectId && pm.UserId != uploaderId)
            .Select(pm => pm.UserId)
            .ToListAsync();

        foreach (var memberUserId in memberUserIds)
        {
            await _notificationService.CreateNotificationAsync(new Notification
            {
                UserId = memberUserId,
                Title = "New project document",
                Message = $"\"{document.Title}\" was added to your project.",
                Type = NotificationType.NewProjectDocument,
                Priority = NotificationPriority.Informational
            });
        }
    }

    private static string GenerateRelativeStoragePath(int userId, int? projectId, string extension)
    {
        // TC-002: {userId}/{projectId-or-"personal"}/{guid}.{extension}
        var projectSegment = projectId.HasValue ? projectId.Value.ToString() : "personal";
        return Path.Combine(userId.ToString(), projectSegment, $"{Guid.NewGuid()}{extension}");
    }

    // OPEN DECISION: Virus Scanning Engine/Library (unresolved). This method is an explicit
    // no-op stub — it does not select or invoke any scanning engine/library. It exists only as
    // the call site FR-007 requires, so integrating a real scanner later does not require
    // re-architecting UploadAsync.
    private static Task<bool> ScanForVirusesAsync(Stream fileStream)
    {
        return Task.FromResult(true);
    }
}

public class DocumentActivityReport
{
    public List<FileTypeCount> MostUploadedTypes { get; set; } = new();
    public List<UploaderCount> MostActiveUploaders { get; set; } = new();
    public List<ActionTypeCount> AccessPatterns { get; set; } = new();
}

public class FileTypeCount
{
    public string FileType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class UploaderCount
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ActionTypeCount
{
    public DocumentActivityType ActionType { get; set; }
    public int Count { get; set; }
}
