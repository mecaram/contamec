namespace ContaMec.Api.Application.SubAccounts.Dto;

public class SubAccountUpdateRequest
{
    public string? Name { get; set; }
    public int? AccountId { get; set; }
    public string? BehaviorType { get; set; }
    public bool Active { get; set; } = true;
}
