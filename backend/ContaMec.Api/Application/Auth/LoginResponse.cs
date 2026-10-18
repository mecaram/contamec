using System.Text.Json.Serialization;

namespace ContaMec.Api.Application.Auth;

public class LoginResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("applicationId")]
    public int ApplicationId { get; set; }

    [JsonPropertyName("applicationName")]
    public string ApplicationName { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>Inicio de sesión anterior al actual, en hora de Argentina y sin zona; null si es el primero.</summary>
    [JsonPropertyName("previousLoginAt")]
    public DateTime? PreviousLoginAt { get; set; }
}
