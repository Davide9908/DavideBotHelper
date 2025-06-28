using Microsoft.EntityFrameworkCore;

namespace DavideBotHelper.Database.Context;

public class DavideBotDbContext : DbContext
{
    
    private readonly IConfiguration _configuration;

    public DavideBotDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(_configuration.GetConnectionString("DavideBotDB"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RepositoryRelease>().Property(c => c.Data).IsRequired(false);
        modelBuilder.Entity<RepositoryRelease>().Property(c => c.AddedAt).IsRequired(false);
        modelBuilder.Entity<RepositoryRelease>().Property(c => c.RequireDownload).HasDefaultValue(false);
        modelBuilder.Entity<RepositoryRelease>().Property(c => c.ToSend).HasDefaultValue(false);
    }
    
    public DbSet<GithubRepository> GithubRepositories { get; set; }
    public DbSet<RepositoryRelease> RepositoryReleases { get; set; }
    
    public void Migrate()
    {
        if (Database.GetPendingMigrations().Any())
        {
            Database.Migrate();
        }
    }
    
}