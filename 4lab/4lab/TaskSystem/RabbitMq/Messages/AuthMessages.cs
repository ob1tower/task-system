using System.Text.Json.Serialization;
using TaskSystem.Dtos.User;

namespace TaskSystem.RabbitMq.Messages;

public class RegisterMessage : BaseMessage
{
    [JsonPropertyName("user_register_dto")]
    public required UserRegisterDto UserRegisterDto { get; set; }
}

public class LoginMessage : BaseMessage
{
    [JsonPropertyName("user_login_dto")]
    public required UserLoginDto UserLoginDto { get; set; }
}

public class RegisterResponseMessage
{
    public Guid MessageId { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? AccessToken { get; set; }
}

public class LoginResponseMessage
{
    public Guid MessageId { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? AccessToken { get; set; }
}