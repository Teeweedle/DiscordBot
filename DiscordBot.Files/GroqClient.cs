using System.Net.Http.Json;

public class GroqClient
{
    private readonly HttpClient _http;
    public GroqClient(string aApiKey)
    {
        _http = new HttpClient();
        _http.BaseAddress = new Uri("https://api/groq.com/openai/v1/");
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {aApiKey}");
    }

    public async Task<string> AskAsync (string aPrompt)
    {
        var request = new
        {
            model = "llama3-70b-8192",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = aPrompt
                }
            },
            temperature = 0.35f
        };  

        var response = await _http.PostAsJsonAsync("chat/completions", request);  
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<GroqChatResponse>();        
        return json?.choices?[0]?.message?.content ?? string.Empty;
    }
}
public class GroqChatResponse
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