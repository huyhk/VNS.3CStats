using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VNS.ThreeCStats.Domain.Competitions;
using VNS.ThreeCStats.Domain.Players;
using VNS.ThreeCStats.Domain.Sources;
using VNS.ThreeCStats.Infrastructure.Identity;

namespace VNS.ThreeCStats.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<PlayerAlias> PlayerAliases => Set<PlayerAlias>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Competition> Competitions => Set<Competition>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<DataSource> DataSources => Set<DataSource>();
    public DbSet<SourceDocument> SourceDocuments => Set<SourceDocument>();
    public DbSet<MatchSource> MatchSources => Set<MatchSource>();
    public DbSet<SourcePlayerIdentity> SourcePlayerIdentities => Set<SourcePlayerIdentity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Player>(entity =>
        {
            entity.Property(x => x.CanonicalName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CountryCode).HasMaxLength(3);
            entity.HasIndex(x => x.CanonicalName);
        });

        builder.Entity<PlayerAlias>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.NormalizedName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Language).HasMaxLength(10);
            entity.HasIndex(x => x.NormalizedName);
            entity.HasOne(x => x.Player)
                .WithMany(x => x.Aliases)
                .HasForeignKey(x => x.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Organization>(entity =>
        {
            entity.Property(x => x.Code).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<Competition>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(250).IsRequired();
            entity.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Tournament>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(300).IsRequired();
            entity.Property(x => x.CountryCode).HasMaxLength(3);
            entity.Property(x => x.City).HasMaxLength(150);
            entity.Property(x => x.SourceExternalId).HasMaxLength(100);
            entity.HasOne(x => x.Competition)
                .WithMany()
                .HasForeignKey(x => x.CompetitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Match>(entity =>
        {
            entity.Property(x => x.Stage).HasMaxLength(100);
            entity.Property(x => x.Group).HasMaxLength(50);
            entity.Property(x => x.Player1Average).HasPrecision(8, 5);
            entity.Property(x => x.Player2Average).HasPrecision(8, 5);
            entity.HasIndex(x => new { x.Player1Id, x.Player2Id });
            entity.HasIndex(x => x.TournamentId);
            entity.HasOne(x => x.Tournament).WithMany().HasForeignKey(x => x.TournamentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Player1).WithMany().HasForeignKey(x => x.Player1Id).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Player2).WithMany().HasForeignKey(x => x.Player2Id).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Winner).WithMany().HasForeignKey(x => x.WinnerId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<DataSource>(entity =>
        {
            entity.Property(x => x.Code).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.BaseUrl).HasMaxLength(500).IsRequired();
            entity.Property(x => x.SourceType).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<SourceDocument>(entity =>
        {
            entity.Property(x => x.ExternalId).HasMaxLength(150);
            entity.Property(x => x.Url).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(100);
            entity.Property(x => x.ContentHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.RawContentPath).HasMaxLength(500);
            entity.Property(x => x.ParserVersion).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => new { x.DataSourceId, x.ExternalId });
            entity.HasOne(x => x.DataSource).WithMany().HasForeignKey(x => x.DataSourceId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MatchSource>(entity =>
        {
            entity.HasKey(x => new { x.MatchId, x.SourceDocumentId });
            entity.Property(x => x.ExternalMatchId).HasMaxLength(150);
            entity.HasOne(x => x.Match).WithMany().HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.SourceDocument).WithMany().HasForeignKey(x => x.SourceDocumentId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SourcePlayerIdentity>(entity =>
        {
            entity.Property(x => x.ExternalPlayerId).HasMaxLength(150).IsRequired();
            entity.Property(x => x.SourceName).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => new { x.DataSourceId, x.ExternalPlayerId }).IsUnique();
            entity.HasOne(x => x.DataSource).WithMany().HasForeignKey(x => x.DataSourceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Player).WithMany().HasForeignKey(x => x.PlayerId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
