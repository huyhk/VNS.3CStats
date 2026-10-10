using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using VNS.ThreeCStats.Domain.Sources;

namespace VNS.ThreeCStats.Infrastructure.Sources.Ceb;

public sealed record CebFetchedDocument(SourceDocument Document, string Content);

public sealed class CebSourceClient(HttpClient httpClient)
{
    public const string SourceCode = "CEB";
    public const string ParserVersion = "ceb-html-v1";

    public async Task<CebFetchedDocument> FetchAsync(
        Uri url,
        string? externalId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(url);

        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType;

        var document = new SourceDocument
        {
            ExternalId = externalId,
            Url = url.AbsoluteUri,
            RetrievedAt = DateTimeOffset.UtcNow,
            ContentType = contentType,
            ContentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant(),
            ParserVersion = ParserVersion
        };

        return new CebFetchedDocument(document, content);
    }

    public static void ConfigureHttpClient(HttpClient client)
    {
        client.BaseAddress = new Uri("https://www.eurobillard.org/");
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("VNS.3CStats", "0.1"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
    }
}
