using System.Net.Http.Headers;
using System.Text;

namespace eShop.Testing.Common;

public static class HttpTrafficLogFormatter
{
    private const int MaxBodyLength = 32_768;

    public static async Task<string> FormatRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        string? body = null;
        if (request.Content is not null)
        {
            body = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        return FormatRequest(request, body);
    }

    public static string FormatRequest(HttpRequestMessage request, string? body)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"{request.Method} {request.RequestUri}");

        AppendHeaders(builder, request.Headers);
        if (request.Content is not null)
        {
            AppendHeaders(builder, request.Content.Headers);
            builder.AppendLine();
            builder.AppendLine(TruncateBody(body ?? string.Empty));
        }
        else
        {
            builder.AppendLine();
            builder.AppendLine("(no body)");
        }

        return builder.ToString().TrimEnd();
    }

    public static async Task<string> FormatResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"{(int)response.StatusCode} {response.ReasonPhrase}");

        AppendHeaders(builder, response.Headers);
        if (response.Content is not null)
        {
            AppendHeaders(builder, response.Content.Headers);
            builder.AppendLine();
            builder.AppendLine(await ReadBodyAsync(response.Content, cancellationToken));
        }
        else
        {
            builder.AppendLine();
            builder.AppendLine("(no body)");
        }

        return builder.ToString().TrimEnd();
    }

    public static string FormatResponse(HttpResponseMessage response, string responseBody)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"{(int)response.StatusCode} {response.ReasonPhrase}");

        AppendHeaders(builder, response.Headers);
        if (response.Content is not null)
        {
            AppendHeaders(builder, response.Content.Headers);
        }

        builder.AppendLine();
        builder.AppendLine(TruncateBody(responseBody));

        return builder.ToString().TrimEnd();
    }

    private static void AppendHeaders(StringBuilder builder, HttpHeaders headers)
    {
        foreach (var header in headers)
        {
            builder.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }
    }

    private static async Task<string> ReadBodyAsync(HttpContent content, CancellationToken cancellationToken)
    {
        var body = await content.ReadAsStringAsync(cancellationToken);
        return TruncateBody(body);
    }

    private static string TruncateBody(string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return "(no body)";
        }

        if (body.Length <= MaxBodyLength)
        {
            return body;
        }

        return body[..MaxBodyLength] + $"... (truncated, total length: {body.Length} characters)";
    }
}
