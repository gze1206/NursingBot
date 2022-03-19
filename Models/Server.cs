using SQLite;

namespace NursingBot.Models
{
    [Table("servers")]
    public class Server
    {
        [PrimaryKey, AutoIncrement, NotNull]
        [Column("id")]
        public ulong Id { get; set; }

        [NotNull, Unique]
        [Column("discordUID")]
        public ulong DiscordUID { get; set; }

        [Column("prefix")]
        public string Prefix { get; set; } = "!";

        [NotNull]
        [Column("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}