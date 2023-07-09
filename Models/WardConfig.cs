using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NursingBot.Models;

[Table("wardConfigs")]
public class WardConfig
{
    [Key]
    public ulong Id { get; set; }

    public ulong ServerId { get; set; }

    [ForeignKey("ServerId")]
    public Server Server { get; set; } = default!;

    public ulong CategoryId { get; set; }

    public ulong HospitalizationId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAt { get; set; }
}