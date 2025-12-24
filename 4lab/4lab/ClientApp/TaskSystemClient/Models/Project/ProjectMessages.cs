using System.Text.Json.Serialization;
using TaskSystemClient.Models.Project;

namespace TaskSystemClient.Models;

public class CreateProjectMessage : JobMessageBase
{
    [JsonPropertyName("project_create_dto")]
    public required ProjectCreateDto ProjectCreateDto { get; set; }
}

public class UpdateProjectMessage : JobMessageBase
{
    [JsonPropertyName("project_id")]
    public Guid ProjectId { get; set; }
    
    [JsonPropertyName("project_update_dto")]
    public required ProjectUpdateDto ProjectUpdateDto { get; set; }
}

public class DeleteProjectMessage : JobMessageBase
{
    [JsonPropertyName("project_id")]
    public Guid ProjectId { get; set; }
}

public class GetProjectMessage : JobMessageBase
{
    [JsonPropertyName("project_id")]
    public Guid ProjectId { get; set; }
}

public class GetAllProjectsMessage : JobMessageBase
{
    [JsonPropertyName("page_number")]
    public int PageNumber { get; set; }

    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }
}