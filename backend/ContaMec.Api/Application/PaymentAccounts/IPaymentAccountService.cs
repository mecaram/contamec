using ContaMec.Api.Application.PaymentAccounts.Dto;

namespace ContaMec.Api.Application.PaymentAccounts;

public interface IPaymentAccountService
{
    Task<List<PaymentAccountResponse>> SearchAsync(PaymentAccountSearchRequest request);
    Task<PaymentAccountResponse?> GetByIdAsync(int id);
    Task<PaymentAccountResponse> CreateAsync(PaymentAccountCreateRequest request);
    Task<bool> UpdateAsync(int id, PaymentAccountUpdateRequest request);
    Task<bool> DeleteAsync(int id);
}
