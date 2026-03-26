using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace dotnet_server.Application.Services;

public class GaugeOcrService(HttpClient httpClient, IOptions<GaugeOcrOptions> options)
{
    private static readonly Regex GaugeNumberRegex = new(@"\b\d{3,6}\b", RegexOptions.Compiled);
    private readonly GaugeOcrOptions _options = options.Value;

    public async Task<(int? reading, string rawText)> ExtractGaugeReadingAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var operationUrl = await StartReadOperationAsync(fileStream, cancellationToken);
        var rawText = await PollReadResultAsync(operationUrl, cancellationToken);
        var parsed = ParseGaugeNumber(rawText);

        return (parsed, rawText);
    }

    private async Task<string> StartReadOperationAsync(Stream fileStream, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildAnalyzeUrl());
        request.Headers.Add("Ocp-Apim-Subscription-Key", _options.ApiKey);
        request.Content = new StreamContent(fileStream);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (!response.Headers.TryGetValues("Operation-Location", out var values))
            throw new InvalidOperationException("OCR did not return an operation location.");

        return values.First();
    }

    private async Task<string> PollReadResultAsync(string operationUrl, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, operationUrl);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _options.ApiKey);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

            var root = document.RootElement;
            var status = root.GetProperty("status").GetString();

            if (string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractRawText(root);
            }

            if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("OCR processing failed.");

            await Task.Delay(500, cancellationToken);
        }

        throw new TimeoutException("OCR processing timed out.");
    }

    private static string ExtractRawText(JsonElement root)
    {
        if (!root.TryGetProperty("analyzeResult", out var analyzeResult)
            || !analyzeResult.TryGetProperty("readResults", out var readResults)
            || readResults.ValueKind != JsonValueKind.Array)
            return string.Empty;

        var lines = new List<string>();
        foreach (var page in readResults.EnumerateArray())
        {
            if (!page.TryGetProperty("lines", out var pageLines) || pageLines.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var line in pageLines.EnumerateArray())
            {
                if (line.TryGetProperty("text", out var textElement) && textElement.ValueKind == JsonValueKind.String)
                    lines.Add(textElement.GetString() ?? string.Empty);
            }
        }

        return string.Join(' ', lines);
    }

    private static int? ParseGaugeNumber(string rawText)
    {
        var matches = GaugeNumberRegex.Matches(rawText);
        if (matches.Count == 0) return null;

        var candidate = matches
            .Select(x => x.Value)
            .OrderByDescending(x => x.Length)
            .ThenByDescending(x => rawText.LastIndexOf(x, StringComparison.Ordinal))
            .FirstOrDefault();

        return int.TryParse(candidate, out var value) ? value : null;
    }

    private string BuildAnalyzeUrl()
    {
        var endpoint = _options.Endpoint.TrimEnd('/');
        return $"{endpoint}/vision/v3.2/read/analyze";
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint) || string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Gauge OCR is not configured.");
    }
}
