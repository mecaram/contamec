namespace ContaMec.Api.Application.SubAccounts.Dto;

public class SubAccountSearchRequest
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public int? AccountId { get; set; }
}
