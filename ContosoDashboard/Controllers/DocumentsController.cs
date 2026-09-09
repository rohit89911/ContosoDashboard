using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ContosoDashboard.Services;
using ContosoDashboard.Models;

namespace ContosoDashboard.Controllers;

// New Controllers folder/MVC controller: justified in plan.md's Complexity Tracking. Files are
// stored outside wwwroot (TC-001) and the stakeholder document explicitly requires controller
// endpoints to serve them so authorization can be enforced (contracts/document-file-endpoints.md).
// Scoped to exactly these two file-serving actions — not a general API layer.
[Authorize]
[Route("documents")]
public class DocumentsController : Controller
{
    private readonly IDocumentService _documentService;
    private readonly IFileStorageService _fileStorageService;

    public DocumentsController(IDocumentService documentService, IFileStorageService fileStorageService)
    {
        _documentService = documentService;
        _fileStorageService = fileStorageService;
    }

    // T022 [US3]: GET /documents/{documentId}/download
    [HttpGet("{documentId:int}/download")]
    public async Task<IActionResult> Download(int documentId)
    {
        var requestingUserId = GetRequestingUserId();
        if (requestingUserId == null) return NotFound();

        var document = await _documentService.GetDocumentForAccessAsync(documentId, requestingUserId.Value);
        if (document == null) return NotFound(); // not found and not-authorized are not distinguished

        var stream = await _fileStorageService.DownloadAsync(document.FilePath);

        await _documentService.LogActivityAsync(document.DocumentId, requestingUserId.Value, DocumentActivityType.Download);

        // Content-Disposition uses the ORIGINAL, user-facing FileName — never the physical
        // GUID-based FilePath (TC-002).
        return File(stream, document.FileType, document.FileName);
    }

    // T023 [US3]: GET /documents/{documentId}/preview
    [HttpGet("{documentId:int}/preview")]
    public async Task<IActionResult> Preview(int documentId)
    {
        var requestingUserId = GetRequestingUserId();
        if (requestingUserId == null) return NotFound();

        var document = await _documentService.GetDocumentForAccessAsync(documentId, requestingUserId.Value);
        if (document == null) return NotFound();

        if (!DocumentFileTypes.IsPreviewable(document.FileName))
        {
            return StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }

        var stream = await _fileStorageService.DownloadAsync(document.FilePath);

        // No Content-Disposition: attachment header, so the browser renders inline rather than
        // downloading (contracts/document-file-endpoints.md).
        return new FileStreamResult(stream, document.FileType);
    }

    private int? GetRequestingUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}
