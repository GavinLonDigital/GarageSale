using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GarageSale.Configuration;
using Microsoft.Extensions.Options;

namespace GarageSale.Services;

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient httpClient;

    public OpenAIService(HttpClient httpClient, IOptions<OpenAIOptions> options)
    {
        this.httpClient = httpClient;
        this.httpClient.BaseAddress ??= new Uri("https://api.openai.com/");
        this.httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", options.Value.OpenAIKey);
    }

    public async Task<string?> GetResponse(string prompt)
    {
        var request = new ResponsesRequest
        {
            Model = "gpt-4o-mini",
            Input = prompt,
        };

        using var httpResponse = await httpClient.PostAsJsonAsync("v1/responses", request);
        if (!httpResponse.IsSuccessStatusCode)
            return null;

        var payload = await httpResponse.Content.ReadFromJsonAsync<ResponsesResponse>();

        var text = payload?.Output?
            .SelectMany(output => output.Content ?? [])
            .FirstOrDefault(content => content.Type == "output_text")
            ?.Text;

        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private sealed class ResponsesRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("input")]
        public string Input { get; set; } = string.Empty;
    }

    private sealed class ResponsesResponse
    {
        [JsonPropertyName("output")]
        public List<ResponsesOutput>? Output { get; set; }
    }

    private sealed class ResponsesOutput
    {
        [JsonPropertyName("content")]
        public List<ResponsesContent>? Content { get; set; }
    }

    private sealed class ResponsesContent
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
