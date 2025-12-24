using System.Text.Json.Serialization;

namespace TaskSystem.RabbitMq.Messages;

public class StandardRequestMessage
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [JsonPropertyName("version")]
    public string Version { get; set; } = "v1";
    
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public object? Data { get; set; }
    
    [JsonPropertyName("auth")]
    public string? Auth { get; set; }
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class StandardResponseMessage
{
    [JsonPropertyName("correlation_id")]
    public Guid CorrelationId { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = "ok";
    
    [JsonPropertyName("data")]
    public object? Data { get; set; }
    
    [JsonPropertyName("error")]
    public string? Error { get; set; }
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}