using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VNS.ThreeCStats.Application.Ingestion;
using VNS.ThreeCStats.Domain.Sources;
using VNS.ThreeCStats.Infrastructure.Normalization;
using VNS.ThreeCStats.Infrastructure.Persistence;
using Xunit;

namespace VNS.ThreeCStats.Infrastructure.Tests.Normalization;

public sealed class PlayerIdentityImporterTests
{
    [Fact]
    public async Task ImportAsync_CreatesCanonicalPlayerAndSourceIdentityOnce()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var source = new DataSource
        {
            Code = "CEB",
            Name = "CEB",
            BaseUrl = "https://www.eurobillard.org/",
            SourceType = "Official"
        };
        db.DataSources.Add(source);
        await db.SaveChangesAsync();

        var resolver = new PlayerIdentityResolver(db);
        var importer = new PlayerIdentityImporter(db, resolver);
        var parsed = new ParsedPlayer("D’AGATA Alessio", null, "ITA");

        var first = await importer.ImportAsync(source.Id, parsed);
        var second = await importer.ImportAsync(source.Id, parsed);

        Assert.Equal(PlayerIdentityResolutionKind.Created, first.Kind);
        Assert.Equal(PlayerIdentityResolutionKind.SourceIdentity, second.Kind);
        Assert.Equal(first.Player?.Id, second.Player?.Id);
        Assert.Equal(1, await db.Players.CountAsync());
        Assert.Equal(1, await db.SourcePlayerIdentities.CountAsync());
        Assert.Equal("D'AGATA ALESSIO", (await db.SourcePlayerIdentities.SingleAsync()).NormalizedName);
    }

    [Fact]
    public async Task ImportAsync_DoesNotCreatePlayerWhenResolutionIsAmbiguous()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var source = new DataSource
        {
            Code = "CEB",
            Name = "CEB",
            BaseUrl = "https://www.eurobillard.org/",
            SourceType = "Official"
        };
        db.DataSources.Add(source);
        db.Players.AddRange(
            new() { CanonicalName = "SAME Player", CountryCode = "NLD" },
            new() { CanonicalName = "SAME   Player", CountryCode = "NLD" });
        await db.SaveChangesAsync();

        var importer = new PlayerIdentityImporter(db, new PlayerIdentityResolver(db));
        var result = await importer.ImportAsync(source.Id, new ParsedPlayer("SAME Player", null, "NLD"));

        Assert.Equal(PlayerIdentityResolutionKind.Ambiguous, result.Kind);
        Assert.Equal(2, await db.Players.CountAsync());
        Assert.Equal(0, await db.SourcePlayerIdentities.CountAsync());
    }
}
