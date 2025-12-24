using System.Text.Json.Serialization;
using TaskSystemClient.Models.User;

namespace TaskSystemClient.Models;

public class RegisterMessage : JobMessageBase
{
    [JsonPropertyName("user_register_dto")]
    public required UserRegisterDto UserRegisterDto { get; set; }
}

public class LoginMessage : JobMessageBase
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