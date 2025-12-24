using System.Text.Json.Serialization;

namespace TaskSystemClient.Models;

public class ProjectResponseMessage
{
    [JsonPropertyName("message_id")]
    public Guid MessageId { get; set; }
    
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    
    [JsonPropertyName("project_id")]
    public Guid? ProjectId { get; set; }
    
    [JsonPropertyName("project")]
    public object? Project { get; set; }
    
    [JsonPropertyName("projects")]
    public object? Projects { get; set; }
    
    [JsonPropertyName("total_count")]
    public int? TotalCount { get; set; }
}

public class ProjectOperationResponse
{
    public Guid MessageId { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid? ProjectId { get; set; }
    public object? Project { get; set; }
    public object? Projects { get; set; }
    public int? TotalCount { get; set; }
}