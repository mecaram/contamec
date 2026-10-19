namespace ContaMec.Api.Application.Accounts.Dto;

public class AccountCreateRequest
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public bool SubAccountRequires { get; set; }
    public string? BehaviorType { get; set; }
    public bool Active { get; set; } = true;
}
