namespace ContaMec.Api.Application.Users.Dto;

public class UserUpdateRequest
{
    public string? Name { get; set; }
    public string? Password { get; set; }
    public bool IsActive { get; set; } = true;
    public int? UserRoleId { get; set; }
}
