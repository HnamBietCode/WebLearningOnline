using Newtonsoft.Json;

namespace LearningManagementSystem.Models
{
    public class ChatMessage
    {
    [JsonProperty("role")]
    public string Role { get; set; }
    [JsonProperty("content")]
    public string Content { get; set; }
    }

    public class GroqRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; }
        [JsonProperty("messages")]
        public List<ChatMessage> Messages { get; set; }
        [JsonProperty("temperature")]
        public double Temperature { get; set; } = 0.7;
        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; } = 1000;
    }

    public class GroqResponse
    {
        public List<Choice> Choices { get; set; }
    }

    public class Choice
    {
        public ChatMessage Message { get; set; }
    }

    public class ChatViewModel
    {
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public string UserInput { get; set; }
    }
}
