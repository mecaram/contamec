namespace ContaMec.Api.Application.SubAccounts.Dto;

public class SubAccountResponse
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int? AccountId { get; set; }
    public string? AccountName { get; set; }
    public string? BehaviorType { get; set; }
    public bool? Active { get; set; }
}
