namespace ContosoDashboard.Services;

/// <summary>
/// Shared file-type rules for the document upload and management feature (FR-002, and the
/// preview restriction in contracts/document-file-endpoints.md). Kept as a small static class,
/// separate from <see cref="DocumentService"/>/<see cref="IDocumentService"/>, so it can be
/// referenced from Razor pages that also inject an <c>IDocumentService DocumentService</c>
/// instance without a name collision.
/// </summary>
public static class DocumentFileTypes
{
    // FR-002: PDF, Office documents (Word/Excel/PowerPoint), text files, JPEG/PNG images.
    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".jpg", ".jpeg", ".png"
    };

    // contracts/document-file-endpoints.md: preview is supported for PDF and images only.
    public static readonly HashSet<string> PreviewableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png"
    };

    public static bool IsAllowedExtension(string extension) => AllowedExtensions.Contains(extension);

    public static bool IsPreviewable(string fileName) => PreviewableExtensions.Contains(Path.GetExtension(fileName));
}
