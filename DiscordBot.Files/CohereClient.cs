using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

public class CohereClient
{
    private readonly HttpClient _http;

    public CohereClient(string apiKey)
    {
        _http = new HttpClient();
        _http.BaseAddress = new Uri("https://api.cohere.ai/v1/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

        /// <summary>
        /// Asks Cohere AI to generate a response based on the given prompt.
        /// </summary>
        /// <param name="aPrompt">The prompt to be sent to Cohere AI.</param>
        /// <returns>The response from Cohere AI, or an empty string if the request failed.</returns>
        /// <exception cref="ArgumentException">If the prompt is null or empty.</exception>
    public async Task<string> AskAsync(string aPrompt)
    {
        if (string.IsNullOrEmpty(aPrompt))
            throw new ArgumentException("Prompt cannot be null or empty.", nameof(aPrompt));

        try
        {
            var request = new CohereChatRequest
            {
                Model = "command-a-03-2025",   
                Message = aPrompt,
                MaxTokens = 300,
                Temperature = 0.7
            };

            HttpResponseMessage lResponse = await _http.PostAsJsonAsync("chat", request);
            string body = await lResponse.Content.ReadAsStringAsync();

            if (!lResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("Cohere API Request Failed:");
                Console.WriteLine(body);
                return string.Empty;
            }
            Console.WriteLine(body);
            var chatResponse = JsonSerializer.Deserialize<CohereChatResponse>(body);

            return chatResponse?.Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Cohere API error:");
            Console.WriteLine(ex.Message);
            return string.Empty;
        }
    }
}
public class CohereChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("chat_history")]
    public List<ChatHistoryItem> ChatHistory { get; set; }

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }
}
public class CohereChatResponse
{
    [JsonPropertyName("response_id")]
    public string ResponseId { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("generation_id")]
    public string GenerationId { get; set; }

    [JsonPropertyName("chat_history")]
    public List<ChatHistoryItem> ChatHistory { get; set; }

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }

    [JsonPropertyName("meta")]
    public Meta Meta { get; set; }
}

public class ChatHistoryItem
{
    [JsonPropertyName("role")]
    public string Role { get; set; }   

    [JsonPropertyName("message")]
    public string Message { get; set; }
}

public class Meta
{
    [JsonPropertyName("api_version")]
    public ApiVersion ApiVersion { get; set; }

    [JsonPropertyName("billed_units")]
    public BilledUnits BilledUnits { get; set; }

    [JsonPropertyName("tokens")]
    public Tokens Tokens { get; set; }

    [JsonPropertyName("cached_tokens")]
    public int CachedTokens { get; set; }
}

public class ApiVersion
{
    [JsonPropertyName("version")]
    public string Version { get; set; }
}

public class BilledUnits
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }
}

public class Tokens
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }
}