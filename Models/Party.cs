using SQLite;

namespace NursingBot.Models
{
    [Table("partyChannels")]
    public class PartyChannel
    {
        [PrimaryKey, AutoIncrement, NotNull]
        [Column("id")]
        public ulong Id { get; set; }

        [Unique, NotNull]
        [Column("serverId")]
        public ulong ServerId { get; set; }

        [NotNull]
        [Column("channelId")]
        public ulong ChannelId { get; set; }

        [NotNull]
        [Column("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotNull]
        [Column("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("partyRecruits")]
    public class PartyRecruit
    {
        [PrimaryKey, AutoIncrement, NotNull]
        [Column("id")]
        public ulong Id { get; set; }

        [NotNull]
        [Column("partyChannelId")]
        public ulong PartyChannelId { get; set; }

        [NotNull, Unique]
        [Column("messageId")]
        public ulong MessageId { get; set; }

        [NotNull]
        [Column("authorId")]
        public ulong AuthorId { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("date")]
        public string? Date { get; set; }

        [NotNull]
        [Column("isClosed")]
        public bool IsClosed { get; set; }

        [NotNull]
        [Column("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotNull]
        [Column("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("closedAt")]
        public DateTime? ClosedAt { get; set; }
    }
}