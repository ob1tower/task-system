using System.Text.Json.Serialization;
using TaskSystem.Dtos.Project;

namespace TaskSystem.RabbitMq.Messages;

public class CreateProjectMessage : BaseMessage
{
    [JsonPropertyName("project_create_dto")]
    public required ProjectCreateDto ProjectCreateDto { get; set; }
}

public class UpdateProjectMessage : BaseMessage
{
    [JsonPropertyName("project_id")]
    public Guid ProjectId { get; set; }
    
    [JsonPropertyName("project_update_dto")]
    public required ProjectUpdateDto ProjectUpdateDto { get; set; }
}

public class DeleteProjectMessage : BaseMessage
{
    [JsonPropertyName("project_id")]
    public Guid ProjectId { get; set; }
}

public class GetProjectMessage : BaseMessage
{
    [JsonPropertyName("project_id")]
    public Guid ProjectId { get; set; }
}

public class GetAllProjectsMessage : BaseMessage
{
    [JsonPropertyName("page_number")]
    public int PageNumber { get; set; }

    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }
}