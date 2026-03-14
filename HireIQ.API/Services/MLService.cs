using System.Text;
using System.Text.Json;

namespace HireIQ.API.Services;

public class MLService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MLService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient Client => 
        _httpClientFactory.CreateClient("FlaskService");

    public async Task<double> QuickScore(
        string resumeText, string jdText)
    {
        var payload = new { resume = resumeText, jd = jdText };
        var response = await PostAsync("/ml/quick-score", payload);
        return response?.GetProperty("score").GetDouble() ?? 0;
    }

    public async Task<object?> DeepAnalyze(
        string resumeText, string jdText)
    {
        var payload = new { resume = resumeText, jd = jdText };
        var response = await PostAsync("/ml/deep-analyze", payload);
        return response;
    }

    public async Task<string> Chat(
        string message, string userId)
    {
        var payload = new { message, user_id = userId };
        var response = await PostAsync("/ml/chat", payload);
        return response?.GetProperty("response")
            .GetString() ?? "Error";
    }

    public async Task<byte[]?> BuildResumePdf(
        Dictionary<string, string> resumeData)
    {
        var client = Client;
        var json = JsonSerializer.Serialize(resumeData);
        var content = new StringContent(
            json, Encoding.UTF8, "application/json"
        );

        var response = await client.PostAsync(
            "/ml/build-resume", content
        );

        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadAsByteArrayAsync();
    }

    private async Task<JsonElement?> PostAsync(
        string endpoint, object payload)
    {
        try
        {
            var client = Client;
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(
                json, Encoding.UTF8, "application/json"
            );

            var response = await client.PostAsync(endpoint, content);
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonElement>(result);
        }
        catch
        {
            return null;
        }
    }
}