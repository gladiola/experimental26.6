using System.ComponentModel.DataAnnotations;

namespace WebAppExperimental266.Models.Main_Objects
{
    public class CrudRecord
    {
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [Required]
        [StringLength(120)]
        public string Title { get; set; } = string.Empty;

        [StringLength(4000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(256)]
        public string OwnerId { get; set; } = string.Empty;

        [StringLength(256)]
        public string OwnerDisplayName { get; set; } = string.Empty;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    }
}
