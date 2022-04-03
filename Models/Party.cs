using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NursingBot.Models
{
    [Table("partyChannels")]
    public class PartyChannel
    {
        [Key]
        public ulong Id { get; set; }

        public ulong ServerId { get; set; }

        [ForeignKey("ServerId")]
        public Server? Server { get; set; }

        public ulong ChannelId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("partyRecruits")]
    public class PartyRecruit
    {
        [Key]
        public ulong Id { get; set; }

        public ulong PartyChannelId { get; set; }

        [ForeignKey("PartyChannelId")]
        public PartyChannel? PartyChannel { get; set; }

        public ulong MessageId { get; set; }

        public ulong AuthorId { get; set; }

        public string? Description { get; set; }

        public string? Date { get; set; }

        public bool IsClosed { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ClosedAt { get; set; }
    }
}