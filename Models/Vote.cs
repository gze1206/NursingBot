using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NursingBot.Models
{
    [Table("votes")]
    public class Vote
    {
        [Key]
        public ulong Id { get; set; }

        public ulong ServerId { get; set; }

        [ForeignKey("ServerId")]
        public Server? Server { get; set; }

        public ulong AuthorId { get; set; }

        public ulong MessageId { get; set; }

        public string? Description { get; set; }

        public string? Choices { get; set; }

        public bool IsClosed { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ClosedAt { get; set; }
    }
}
