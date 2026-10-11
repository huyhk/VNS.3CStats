using VNS.ThreeCStats.Infrastructure.Sources.Ceb;
using Xunit;

namespace VNS.ThreeCStats.Infrastructure.Tests.Sources.Ceb;

public sealed class CebPlayerParserTests
{
    [Fact]
    public void ParsePlayers_ExtractsSourceIdentityAndDeduplicatesPlayerLinks()
    {
        const string html = """
            <html><body>
                <div data-country="DNK">
                    <a href="/players/kristiansen-daniel-123.html">KRISTIANSEN Daniel</a>
                </div>
                <div data-country-code="NLD">
                    <a href="https://www.eurobillard.org/players/van-t-zelfden-joris-456.html?tab=results">VAN 'T ZELFDEN Joris</a>
                </div>
                <a href="/players/kristiansen-daniel-123.html">KRISTIANSEN Daniel</a>
            </body></html>
            """;

        var players = new CebPlayerParser().ParsePlayers(html);

        Assert.Equal(2, players.Count);
        Assert.Equal("KRISTIANSEN Daniel", players[0].SourceName);
        Assert.Equal("kristiansen-daniel-123", players[0].ExternalPlayerId);
        Assert.Equal("DNK", players[0].CountryCode);
        Assert.Equal("VAN 'T ZELFDEN Joris", players[1].SourceName);
        Assert.Equal("van-t-zelfden-joris-456", players[1].ExternalPlayerId);
        Assert.Equal("NLD", players[1].CountryCode);
    }

    [Fact]
    public void ParsePlayers_IgnoresLinksWithoutPlayerIdentity()
    {
        const string html = """
            <html><body>
                <a href="/events/ec3cu25-460.html">Event</a>
                <a href="/players/">Players</a>
                <a href="/players/valid-player-99.html">VALID Player</a>
            </body></html>
            """;

        var players = new CebPlayerParser().ParsePlayers(html);

        var player = Assert.Single(players);
        Assert.Equal("valid-player-99", player.ExternalPlayerId);
    }
}
