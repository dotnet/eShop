namespace eShop.Testing.Common;

public sealed class HttpTrafficLoggingHandler : DelegatingHandler
{
    public HttpTrafficLoggingHandler()
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? requestBody = null;
        if (request.Content is not null)
        {
            requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            request.Content = RecreateContent(request.Content, requestBody);
        }

        var requestLog = HttpTrafficLogFormatter.FormatRequest(request, requestBody);
        TestOutputWriter.WriteLine($"===== HTTP REQUEST ====={Environment.NewLine}{requestLog}");

        var response = await base.SendAsync(request, cancellationToken);

        string responseBody = string.Empty;
        if (response.Content is not null)
        {
            responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            response.Content = RecreateContent(response.Content, responseBody);
        }

        var responseLog = HttpTrafficLogFormatter.FormatResponse(response, responseBody);
        TestOutputWriter.WriteLine($"===== HTTP RESPONSE ====={Environment.NewLine}{responseLog}");

        return response;
    }

    private static HttpContent RecreateContent(HttpContent originalContent, string body)
    {
        var contentType = originalContent.Headers.ContentType;
        var mediaType = contentType?.MediaType ?? "application/json";
        var newContent = new StringContent(body, System.Text.Encoding.UTF8, mediaType);

        if (contentType?.CharSet is not null)
        {
            newContent.Headers.ContentType!.CharSet = contentType.CharSet;
        }

        foreach (var header in originalContent.Headers)
        {
            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)
                || header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return newContent;
    }
}
