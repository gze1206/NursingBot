using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NursingBot.Logger;
using NursingBot.Models;

namespace NursingBot.Core;

public static class Database
{
#pragma warning disable
    public static PooledDbContextFactory<ApplicationDbContext> Instance { get; private set; }
#pragma warning enable

    public static Dictionary<ulong, Server> CachedServers { get; private set; } = new();

    public static async Task Initialize(string connectionString)
    {
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
                .LogTo(log => Log.Fatal(log), Microsoft.Extensions.Logging.LogLevel.Error)
                .Options;

            Instance = new PooledDbContextFactory<ApplicationDbContext>(options);

            await using var context = await Instance.CreateDbContextAsync();
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    public static void Cache(ulong guildId, Server server)
    {
        CachedServers[guildId] = server;
    }

    public static void ClearCache(ulong guildId)
    {
        CachedServers.Remove(guildId);
    }
}

public partial class ApplicationDbContext : DbContext
{
    public DbSet<Server> Servers { get; private set; }
    public DbSet<PartyChannel> PartyChannels { get; private set; }
    public DbSet<PartyRecruit> PartyRecruits { get; private set; }
    public DbSet<Vote> Votes { get; private set; }
    public DbSet<WardConfig> WardConfigs { get; private set; }
    public DbSet<RoleManager> RoleManagers { get; private set; }
    public DbSet<Role> Roles { get; private set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Server>()
            .HasIndex(s => s.DiscordUID);

        modelBuilder.Entity<PartyChannel>()
            .HasOne<Server>(ch => ch.Server);

        modelBuilder.Entity<PartyRecruit>()
            .HasOne<PartyChannel>(pr => pr.PartyChannel);

        modelBuilder.Entity<Vote>()
            .HasOne<Server>(v => v.Server);

        modelBuilder.Entity<WardConfig>()
            .HasOne<Server>(w => w.Server);

        modelBuilder.Entity<RoleManager>()
            .HasOne<Server>(r => r.Server);

        modelBuilder.Entity<RoleManager>()
            .HasMany<Role>(r => r.Roles)
            .WithOne(r => r.RoleManager)
            .HasForeignKey(r => r.RoleManagerId)
            .IsRequired();

        base.OnModelCreating(modelBuilder);
    }
}