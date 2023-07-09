using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

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

    public ICollection<Role> Roles { get; set; } = new List<Role>();
    
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
    public byte[] Emoji { get; set; } = null!;
    [Required, Comment("유니코드 코드 페이지 문제인지 string을 사용해서 emoji의 정상적인 쿼리가 불가합니다. 바이트로 인코딩해서 쿼리하되, 디버깅 시 편의성을 위해 문자열은 함께 기록합니다.")]
    public string EmojiForDebug { get; set; } = null!;
    
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}