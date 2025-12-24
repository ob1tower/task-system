using System.Text.Json.Serialization;

namespace TaskSystem.RabbitMq.Messages;

public class ResponseMessage
{
    [JsonPropertyName("message_id")]
    public Guid MessageId { get; set; }
    
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    
    [JsonPropertyName("data")]
    public object? Data { get; set; }
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("operation")]
    public string? Operation { get; set; }
}