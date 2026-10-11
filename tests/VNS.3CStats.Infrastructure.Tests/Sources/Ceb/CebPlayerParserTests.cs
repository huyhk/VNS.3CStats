using VNS.ThreeCStats.Infrastructure.Sources.Ceb;
using Xunit;

namespace VNS.ThreeCStats.Infrastructure.Tests.Sources.Ceb;

public sealed class CebPlayerParserTests
{
    [Fact]
    public void ParsePlayers_ParsesCurrentCebRosterMarkup()
    {
        const string html = """
            <html><body>
            <div id="onglet_6" class="onglet_texte">
                <div class="box_ligne box_titres">
                    <div class="c_15 center col-s">Position</div>
                    <div class="c_35 center col-s">Player</div>
                    <div class="c_15 center col-s">Status</div>
                    <div class="c_15 center col-s">Comment</div>
                </div>
                <div class="box_ligne">
                    <div class="c_15 center col-s"><em>1</em></div>
                    <div class="c_35 center col-s">MORTENSEN Lukas (DNK)</div>
                    <div class="c_15 center col-s"></div>
                    <div class="c_15 center col-s"><em>Wild Card Organizer</em></div>
                </div>
                <div class="box_ligne">
                    <div class="c_15 center col-s"><em>4</em></div>
                    <div class="c_35 center col-s">D’AGATA Alessio (ITA)</div>
                    <div class="c_15 center col-s"></div>
                    <div class="c_15 center col-s"><em>ITA1</em></div>
                </div>
                <div class="box_ligne">
                    <div class="c_15 center col-s"><em>14</em></div>
                    <div class="c_35 center col-s">CRISCINO Emanuele (IT)</div>
                    <div class="c_15 center col-s"></div>
                    <div class="c_15 center col-s"><em>ITA2</em></div>
                </div>
                <div class="box_ligne">
                    <div class="c_15 center col-s"><em>Reserve 1</em></div>
                    <div class="c_35 center col-s">NIELSEN Tais-holger (DNK)</div>
                    <div class="c_15 center col-s">Reserve</div>
                    <div class="c_15 center col-s"><em>DNK3</em></div>
                </div>
            </div>
            </body></html>
            """;

        var players = new CebPlayerParser().ParsePlayers(html);

        Assert.Equal(4, players.Count);
        Assert.Equal("MORTENSEN Lukas", players[0].SourceName);
        Assert.Equal("DNK", players[0].CountryCode);
        Assert.Null(players[0].ExternalPlayerId);
        Assert.Equal("D’AGATA Alessio", players[1].SourceName);
        Assert.Equal("ITA", players[1].CountryCode);
        Assert.Equal("CRISCINO Emanuele", players[2].SourceName);
        Assert.Equal("ITA", players[2].CountryCode);
        Assert.Equal("NIELSEN Tais-holger", players[3].SourceName);
        Assert.Equal("DNK", players[3].CountryCode);
    }

    [Fact]
    public void ParsePlayers_ReturnsEmpty_WhenRosterTabIsMissing()
    {
        const string html = "<html><body><div class=\"box_ligne\"><div>Other tab</div></div></body></html>";

        var players = new CebPlayerParser().ParsePlayers(html);

        Assert.Empty(players);
    }
}
