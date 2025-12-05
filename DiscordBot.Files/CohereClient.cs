using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

public class CohereClient
{
    private readonly HttpClient _http;

    public CohereClient(string apiKey)
    {
        _http = new HttpClient();
        _http.BaseAddress = new Uri("https://api.cohere.com/v1/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> AskAsync(string aPrompt)
    {
        if (string.IsNullOrEmpty(aPrompt))
            throw new ArgumentException("Prompt cannot be null or empty.", nameof(aPrompt));

        try
        {
            var request = new
            {
                model = "command-r-plus",   
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = aPrompt
                    }
                },
                max_tokens = 300,
                temperature = 0.3
            };
            string jsonString = JsonSerializer.Serialize(request);
            StringContent content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            HttpResponseMessage lResponse = await _http.PostAsJsonAsync("chat", content);

            string body = await lResponse.Content.ReadAsStringAsync();

            if (!lResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("Cohere API Request Failed:");
                Console.WriteLine(body);
                return string.Empty;
            }

            // Deserialize
            CohereChatResponse? json = JsonSerializer.Deserialize<CohereChatResponse>(body);

            return json?.choices?[0]?.message?.content ?? string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Cohere API error:");
            Console.WriteLine(ex.Message);
            return string.Empty;
        }
    }
}

public class CohereChatResponse
{
    public List<Choice>? choices { get; set; }
}

public class Choice
{
    public Message? message { get; set; }
}

public class Message
{
    public string? role { get; set; }
    public string? content { get; set; }
}
