namespace ContaMec.Api.Application.PaymentAccounts.Dto;

public class PaymentAccountCreateRequest
{
    public string? Name { get; set; }
    public bool Active { get; set; } = true;
}
