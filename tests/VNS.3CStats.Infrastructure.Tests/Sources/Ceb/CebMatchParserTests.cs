using VNS.ThreeCStats.Infrastructure.Sources.Ceb;
using Xunit;

namespace VNS.ThreeCStats.Infrastructure.Tests.Sources.Ceb;

public sealed class CebMatchParserTests
{
    [Fact]
    public void ParseRow_ParsesOfficialCebMatchMarkup()
    {
        var match = new CebMatchParser().ParseRow(MatchRow);

        Assert.Equal(new DateTimeOffset(2025, 3, 28, 9, 30, 0, TimeSpan.Zero), match.PlayedAt);
        Assert.Equal(1, match.MatchNumber);
        Assert.Equal(1, match.TableNumber);
        Assert.Equal("Qualifications", match.Stage);
        Assert.Equal("A", match.Group);
        Assert.Equal("KRISTIANSEN Daniel", match.Player1.SourceName);
        Assert.Equal("VAN 'T ZELFDEN Joris", match.Player2.SourceName);
        Assert.Equal(0, match.Player1MatchPoints);
        Assert.Equal(2, match.Player2MatchPoints);
        Assert.Equal(15, match.Player1Score);
        Assert.Equal(30, match.Player2Score);
        Assert.Equal(28, match.Innings);
        Assert.Equal(0.535m, match.Player1Average);
        Assert.Equal(1.071m, match.Player2Average);
        Assert.Equal(2, match.Player1HighRun);
        Assert.Equal(6, match.Player2HighRun);
    }

    [Fact]
    public void ParseMatches_IgnoresOtherBoxLinesAndExtractsMatchRows()
    {
        var html = $"""
            <html><body>
                <div class="box_ligne"><div>Info row</div></div>
                <section id="matches">
                    {MatchRow}
                    {MatchRow.Replace(">1</div>\n    <div class=\"c_05 center col-s\">1</div>", ">2</div>\n    <div class=\"c_05 center col-s\">2</div>")}
                </section>
                <div class="box_ligne"><div>Classification row</div></div>
            </body></html>
            """;

        var matches = new CebMatchParser().ParseMatches(html);

        Assert.Equal(2, matches.Count);
        Assert.Equal(1, matches[0].MatchNumber);
        Assert.Equal(2, matches[1].MatchNumber);
        Assert.Equal(2, matches[1].TableNumber);
    }

    [Fact]
    public void ParseRow_RejectsUnexpectedColumnLayout()
    {
        const string html = "<div class=\"box_ligne\"><div>only one column</div></div>";

        var exception = Assert.Throws<FormatException>(() => new CebMatchParser().ParseRow(html));

        Assert.Contains("Expected 11", exception.Message);
    }

    private const string MatchRow = """
        <div class="box_ligne">
            <div class="c_10 col-s">28-03-2025 09:30</div>
            <div class="c_05 center col-s">1</div>
            <div class="c_05 center col-s">1</div>
            <div class="c_10 center col-s">Qualifications</div>
            <div class="c_05 center col-s">A</div>
            <div class="c_25 col-s">
                <span class="bleu">KRISTIANSEN Daniel</span><br>
                <span class="rouge">VAN 'T ZELFDEN Joris</span>
            </div>
            <div class="c_05 center col-s">
                <span class="bleu">0</span><br><span class="rouge">2</span>
            </div>
            <div class="c_10 center col-s">
                <span class="bleu">15</span><br><span class="rouge">30</span>
            </div>
            <div class="c_05 center col-s">
                <span class="bleu">28</span><br><span class="rouge">28</span>
            </div>
            <div class="c_10 center col-s">
                <span class="bleu">0.535</span><br><span class="rouge">1.071</span>
            </div>
            <div class="c_05 center col-s">
                <span class="bleu">2</span><br><span class="rouge">6</span>
            </div>
        </div>
        """;
}
