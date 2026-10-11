namespace VNS.ThreeCStats.Infrastructure.Sources.Ceb;

public sealed class CebEventDiscoveryClient(HttpClient httpClient, CebEventDiscoveryParser parser)
{
    public static readonly Uri CalendarUri = new("https://www.eurobillard.org/pages/events-29.html");

    public async Task<IReadOnlyList<DiscoveredCebEvent>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(CalendarUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        return parser.Parse(html);
    }

    public static void ConfigureHttpClient(HttpClient client)
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("VNS.3CStats/1.0 (+CEB results indexing)");
    }
}
