namespace ContaMec.Api.Application.Users.Dto;

public class UserCreateRequest
{
    public string? Name { get; set; }
    public string? Password { get; set; }
    public bool IsActive { get; set; } = true;
    public int? UserRoleId { get; set; }

    /// <summary>Empresa del nuevo usuario. Solo un SuperAdministrador puede indicar una distinta de la propia.</summary>
    public int? ApplicationId { get; set; }
}
