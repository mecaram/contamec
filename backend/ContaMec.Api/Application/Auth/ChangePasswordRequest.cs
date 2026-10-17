using System.Text.Json.Serialization;

namespace ContaMec.Api.Application.Auth;

public class ChangePasswordRequest
{
    [JsonPropertyName("currentPassword")]
    public string? CurrentPassword { get; set; }

    [JsonPropertyName("newPassword")]
    public string? NewPassword { get; set; }

    [JsonPropertyName("confirmPassword")]
    public string? ConfirmPassword { get; set; }
}
