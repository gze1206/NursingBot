using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NursingBot.Models;

[Table("roleManagers")]
public class RoleManager
{
    [Key]
    public ulong Id { get; set; }
    
    public ulong ServerId { get; set; }
    
    [ForeignKey("ServerId")]
    public Server? Server { get; set; }
    
    public ulong ChannelId { get; set; }
    
    public ulong MessageId { get; set; }
    
    [Required]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public Role[] Roles { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("roles")]
public class Role
{
    [Key]
    public ulong Id { get; set; }
    
    public ulong RoleManagerId { get; set; }

    [ForeignKey("RoleManagerId")]
    public RoleManager RoleManager { get; set; } = null!;
    
    public ulong DiscordRoleId { get; set; }

    [Required]
    public string Emoji { get; set; } = null!;
    
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}