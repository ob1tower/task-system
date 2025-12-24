using System.Text.Json.Serialization;
using TaskSystemClient.Models.Job;
using TaskSystemClient.Models.User;

namespace TaskSystemClient.Models;

// Define message classes
public class BaseMessage
{
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Operation { get; set; } = string.Empty;
}

public class JobMessageBase : BaseMessage
{
}

public class CreateJobMessage : JobMessageBase
{
    [JsonPropertyName("job_create_dto")]
    public required JobCreateDto JobCreateDto { get; set; }
}

public class UpdateJobMessage : JobMessageBase
{
    [JsonPropertyName("job_id")]
    public Guid JobId { get; set; }

    [JsonPropertyName("job_update_dto")]
    public required JobUpdateDto JobUpdateDto { get; set; }
}

public class DeleteJobMessage : JobMessageBase
{
    [JsonPropertyName("job_id")]
    public Guid JobId { get; set; }
}

public class GetJobMessage : JobMessageBase
{
    [JsonPropertyName("job_id")]
    public Guid JobId { get; set; }
}

public class GetAllJobsMessage : JobMessageBase
{
    [JsonPropertyName("page_number")]
    public int PageNumber { get; set; }

    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }
}