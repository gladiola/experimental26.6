using System.ComponentModel.DataAnnotations;

namespace WebAppExperimental266.Models.Main_Objects
{
    public class CrudRecord
    {
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [StringLength(120)]
        public string? Title { get; set; }

        [StringLength(4000)]
        public string Description { get; set; } = string.Empty;

        [StringLength(260)]
        public string? UploadedFileName { get; set; }

        [StringLength(120)]
        public string? UploadedContentType { get; set; }

        public long? UploadedFileSizeBytes { get; set; }

        public byte[]? UploadedFileContent { get; set; }

        [StringLength(64)]
        public string CardType { get; set; } = "MIFARE Classic";

        public bool IsPublic { get; set; } = false;

        public bool UploadPermissionConfirmed { get; set; } = false;

        [Required]
        [StringLength(256)]
        public string OwnerId { get; set; } = string.Empty;

        [StringLength(256)]
        public string OwnerDisplayName { get; set; } = string.Empty;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    }
}
