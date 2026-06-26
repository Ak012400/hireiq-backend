using System.Text;
using System.Text.Json;

namespace HireIQ.API.Services;

public class MLService
{
    private readonly HttpClient _client;
    private readonly string _hfToken;
    private const string BASE_URL =
        "https://98012arun-minilm.hf.space";

    public MLService(IConfiguration config)
    {
        _hfToken = config["HFSettings:Token"] ??
                   Environment.GetEnvironmentVariable("HF_TOKEN") ?? "";
        _client = new HttpClient();
        _client.Timeout = TimeSpan.FromSeconds(60);
        _client.DefaultRequestHeaders.Add(
            "Authorization", $"Bearer {_hfToken}"
        );
    }

    public async Task<double> QuickScore(
        string resumeText, string jdText)
    {
        try
        {
            // Step 1: POST karo event_id lo
            var payload = new
            {
                data = new[] { resumeText, jdText }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(
                json, Encoding.UTF8, "application/json"
            );

            var postResponse = await _client.PostAsync(
                $"{BASE_URL}/gradio_api/call/score_resume",
                content
            );

            var postResult = await postResponse.Content
                .ReadAsStringAsync();

            var eventData = JsonSerializer
                .Deserialize<JsonElement>(postResult);
            var eventId = eventData
                .GetProperty("event_id")
                .GetString();

            // Step 2: GET result
            var getResponse = await _client.GetAsync(
                $"{BASE_URL}/gradio_api/call/score_resume/{eventId}"
            );

            var getResult = await getResponse.Content
                .ReadAsStringAsync();

            // Parse SSE response
            var lines = getResult.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);
                    var parsed = JsonSerializer
                        .Deserialize<JsonElement>(data);
                    if (parsed.ValueKind == JsonValueKind.Array)
                    {
                        var scoreObj = parsed[0];
                        return scoreObj
                            .GetProperty("score")
                            .GetDouble();
                    }
                }
            }
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine($"MiniLM error: {e.Message}");
            return 0;
        }
    }
}