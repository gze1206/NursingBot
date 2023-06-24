using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NursingBot.Models;

[Table("servers")]
public class Server
{
    [Key]
    public ulong Id { get; set; }

    public ulong DiscordUID { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}