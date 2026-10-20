using System.Text.Json;
using System.Text.RegularExpressions;

static class Rubis
{
    const string DefaultUploadUrl = "https://api.rubis.app/v2/scrap?public=true";

    public static async Task<string> Upload(string payload, CancellationToken token = default)
    {
        return await Upload(payload, DefaultUploadUrl, token);
    }

    public static async Task<string> Upload(string payload, string url, CancellationToken token = default)
    {
        using var client = new HttpClient();
        var content = new StringContent(payload, System.Text.Encoding.UTF8, "text/plain");

        try
        {
            var response = await client.PostAsync(url, content, token);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(token);
            var json = JsonDocument.Parse(responseBody);

            if (!json.RootElement.TryGetProperty("success", out var successProp) || !successProp.GetBoolean())
                throw new InvalidOperationException("Upload failed: server reported unsuccessful operation");

            if (!json.RootElement.TryGetProperty("view_with_key", out var viewWithKeyProp))
                throw new InvalidOperationException("Upload failed: response missing view_with_key field");

            return viewWithKeyProp.GetString() ?? throw new InvalidOperationException("Upload failed: view_with_key is null");
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Network error during upload: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid response from server: {ex.Message}", ex);
        }
    }

    public static async Task<byte[]> Receive(string url, CancellationToken token = default)
    {
        using var client = new HttpClient();

        try
        {
            var fetchUrl = ConvertToRawUrl(url);
            var response = await client.GetAsync(fetchUrl, token);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync(token);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Network error during download: {ex.Message}", ex);
        }
    }

    static string ConvertToRawUrl(string url)
    {
        if (!url.Contains("rubis.app/view/"))
            return url;

        var match = Regex.Match(url, @"scrap=([^&]+)");
        if (!match.Success)
            return url;

        var scrapId = match.Groups[1].Value;
        return $"https://api.rubis.app/v2/scrap/{scrapId}/raw";
    }
}
