using LearningManagementSystem.Models;
using Newtonsoft.Json;
using System.Text;

public interface IGroqService
{
    Task<string> GetChatResponseAsync(List<ChatMessage> messages);
}

public class GroqService : IGroqService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GroqService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GroqApi:ApiKey"];
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<string> GetChatResponseAsync(List<ChatMessage> messages)
    {
        try
        {
            var systemMessage = new ChatMessage
            {
                Role = "system",
                Content = @"Bạn là một chatbot chuyên tư vấn khóa học trực tuyến..."
            };

            var allMessages = new List<ChatMessage> { systemMessage };
            allMessages.AddRange(messages);

            var request = new GroqRequest
            {
                Model = "llama3-8b-8192", // Model chính xác
                Messages = allMessages,
                Temperature = 0.7,
                MaxTokens = 1000
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var groqResponse = JsonConvert.DeserializeObject<GroqResponse>(responseString);
                return groqResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? "Xin lỗi, tôi không thể trả lời lúc này.";
            }
            else
            {
                return $"Lỗi: {response.StatusCode} - {responseString}";
            }
        }
        catch (Exception ex)
        {
            return $"Có lỗi xảy ra: {ex.Message}";
        }
    }
}