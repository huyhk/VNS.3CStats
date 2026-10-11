using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VNS.ThreeCStats.Domain.Players;
using VNS.ThreeCStats.Domain.Sources;
using VNS.ThreeCStats.Infrastructure.Normalization;
using VNS.ThreeCStats.Infrastructure.Persistence;
using Xunit;

namespace VNS.ThreeCStats.Infrastructure.Tests.Normalization;

public sealed class PlayerIdentityResolverTests
{
    [Fact]
    public async Task ResolveAsync_UsesConfirmedSourceIdentityWithoutExternalId()
    {
        await using var fixture = await ResolverFixture.CreateAsync();
        var player = await fixture.AddPlayerAsync("D'AGATA Alessio", "ITA");
        fixture.Db.SourcePlayerIdentities.Add(new SourcePlayerIdentity
        {
            DataSourceId = fixture.Source.Id,
            ExternalPlayerId = null,
            SourceName = "D’AGATA Alessio",
            NormalizedName = PlayerNameNormalizer.Normalize("D’AGATA Alessio"),
            CountryCode = "ITA",
            PlayerId = player.Id
        });
        await fixture.Db.SaveChangesAsync();

        var result = await fixture.Resolver.ResolveAsync(
            fixture.Source.Id, null, "D'AGATA   Alessio", "ita");

        Assert.Equal(PlayerIdentityResolutionKind.SourceIdentity, result.Kind);
        Assert.Equal(player.Id, result.Player?.Id);
    }

    [Fact]
    public async Task ResolveAsync_ResolvesUniqueExactNormalizedNameAndCountry()
    {
        await using var fixture = await ResolverFixture.CreateAsync();
        var player = await fixture.AddPlayerAsync("VAN 'T ZELFDEN Joris", "NLD");

        var result = await fixture.Resolver.ResolveAsync(
            fixture.Source.Id, null, "VAN 'T   ZELFDEN Joris", "NLD");

        Assert.Equal(PlayerIdentityResolutionKind.ExactNameAndCountry, result.Kind);
        Assert.Equal(player.Id, result.Player?.Id);
    }

    [Fact]
    public async Task ResolveAsync_DoesNotResolveSameNameFromDifferentCountry()
    {
        await using var fixture = await ResolverFixture.CreateAsync();
        await fixture.AddPlayerAsync("SAME Player", "BEL");

        var result = await fixture.Resolver.ResolveAsync(
            fixture.Source.Id, null, "SAME Player", "NLD");

        Assert.Equal(PlayerIdentityResolutionKind.Unresolved, result.Kind);
        Assert.Null(result.Player);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsAmbiguousWhenMultiplePlayersShareExactIdentityKey()
    {
        await using var fixture = await ResolverFixture.CreateAsync();
        await fixture.AddPlayerAsync("SAME Player", "NLD");
        await fixture.AddPlayerAsync("SAME   Player", "NLD");

        var result = await fixture.Resolver.ResolveAsync(
            fixture.Source.Id, null, "SAME Player", "NLD");

        Assert.Equal(PlayerIdentityResolutionKind.Ambiguous, result.Kind);
        Assert.Null(result.Player);
        Assert.Equal(2, result.Candidates.Count);
    }

    private sealed class ResolverFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private ResolverFixture(SqliteConnection connection, AppDbContext db, DataSource source)
        {
            _connection = connection;
            Db = db;
            Source = source;
            Resolver = new PlayerIdentityResolver(db);
        }

        public AppDbContext Db { get; }
        public DataSource Source { get; }
        public PlayerIdentityResolver Resolver { get; }

        public static async Task<ResolverFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;
            var db = new AppDbContext(options);
            await db.Database.EnsureCreatedAsync();

            var source = new DataSource
            {
                Code = "CEB",
                Name = "Confédération Européenne de Billard",
                BaseUrl = "https://www.eurobillard.org/",
                SourceType = "Official"
            };
            db.DataSources.Add(source);
            await db.SaveChangesAsync();
            return new ResolverFixture(connection, db, source);
        }

        public async Task<Player> AddPlayerAsync(string canonicalName, string countryCode)
        {
            var player = new Player
            {
                CanonicalName = canonicalName,
                CountryCode = countryCode
            };
            Db.Players.Add(player);
            await Db.SaveChangesAsync();
            return player;
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
