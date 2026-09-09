using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentActivityLog
{
    [Key]
    public int LogId { get; set; }

    // Nullable (not [Required]): permanent document deletion (FR-014) must not be blocked by,
    // or cascade-delete, the audit trail (FR-021). The FK is set null via DeleteBehavior.SetNull
    // (ApplicationDbContext) when the referenced Document is permanently deleted, so this log row
    // — including the Delete action itself — survives as a dangling/soft reference.
    public int? DocumentId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public DocumentActivityType ActionType { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("DocumentId")]
    public virtual Document? Document { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}

public enum DocumentActivityType
{
    Upload,
    Download,
    Delete,
    Share
}
