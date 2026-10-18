using System.Text.Json.Serialization;

namespace ContaMec.Api.Application.Auth;

public class RefreshTokenResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}
