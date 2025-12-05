// using System.Net.Http.Json;

// public class GroqClient
// {
//     private readonly HttpClient _http;
//     public GroqClient(string aApiKey)
//     {
//         _http = new HttpClient();
//         _http.BaseAddress = new Uri("https://api.groq.com/");
//         _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {aApiKey}");
//     }

//     public async Task<string> AskAsync (string aPrompt)
//     {
//         var request = new
//         {
//             model = "meta-llama/llama-4-maverick-17b-128e-instruct",
//             messages = new[]
//             {
//                 new
//                 {
//                     role = "user",
//                     content = aPrompt
//                 }
//             }
//         };  

//         try
//         {
//             HttpResponseMessage response = await _http.PostAsJsonAsync("openai/v1", request);  
//             // response.EnsureSuccessStatusCode();
//             string responseBody = await response.Content.ReadAsStringAsync();
//             if (!response.IsSuccessStatusCode)
//             {
//                 Console.WriteLine("Groq API request failed!");
//                 Console.WriteLine("Status code: " + response.StatusCode);
//                 Console.WriteLine("Response body: " + responseBody);
//                 return string.Empty;
//             } 
//             var json = await response.Content.ReadFromJsonAsync<GroqChatResponse>();        
//             return json?.choices?[0]?.message?.content ?? string.Empty;
//         }
//         catch (HttpRequestException ex)
//         {
//             Console.WriteLine($"[HttpRequest Error] {ex.GetType().Name}: {ex.Message}");
//             throw;
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine($"[Unexpected Error] {ex.GetType().Name}: {ex.Message}");
//             throw;
//         }
//     }
// }
// public class GroqChatResponse
// {
//     public List<Choice>? choices { get; set; }
// }
// public class Choice
// {
//     public Message? message { get; set; }
// }
// public class Message
// {
//     public string? role { get; set; }
//     public string? content { get; set; }
// }