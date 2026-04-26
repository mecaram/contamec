namespace ContaMec.Api.Application.Users.Dto;

public class UserResponse
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsActive { get; set; }
    public int? UserRoleId { get; set; }
    public string? UserRoleName { get; set; }
}
