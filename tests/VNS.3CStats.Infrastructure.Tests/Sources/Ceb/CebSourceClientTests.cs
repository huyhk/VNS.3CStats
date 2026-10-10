using System.Net;
using System.Text;
using VNS.ThreeCStats.Infrastructure.Sources.Ceb;
using Xunit;

namespace VNS.ThreeCStats.Infrastructure.Tests.Sources.Ceb;

public sealed class CebSourceClientTests
{
    [Fact]
    public async Task FetchAsync_ReturnsContentAndSourceMetadata()
    {
        const string html = "<html><body>CEB tournament</body></html>";
        var handler = new StubHttpMessageHandler(html, "text/html");
        var httpClient = new HttpClient(handler);
        CebSourceClient.ConfigureHttpClient(httpClient);
        var client = new CebSourceClient(httpClient);
        var url = new Uri("https://www.eurobillard.org/test-tournament");

        var fetched = await client.FetchAsync(url, "479");

        Assert.Equal(html, fetched.Content);
        Assert.Equal("479", fetched.Document.ExternalId);
        Assert.Equal(url.AbsoluteUri, fetched.Document.Url);
        Assert.Equal("text/html", fetched.Document.ContentType);
        Assert.Equal(CebSourceClient.ParserVersion, fetched.Document.ParserVersion);
        Assert.Equal(64, fetched.Document.ContentHash.Length);
        Assert.NotEqual(default, fetched.Document.RetrievedAt);
    }

    private sealed class StubHttpMessageHandler(string content, string mediaType) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent(content, Encoding.UTF8, mediaType)
            };

            return Task.FromResult(response);
        }
    }
}
