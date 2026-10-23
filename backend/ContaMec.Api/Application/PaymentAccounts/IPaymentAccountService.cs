using ContaMec.Api.Application.Orders;
using ContaMec.Api.Application.PaymentAccounts.Dto;

namespace ContaMec.Api.Application.PaymentAccounts;

public interface IPaymentAccountService
{
    Task<List<PaymentAccountResponse>> SearchAsync(PaymentAccountSearchRequest request, int applicationId);
    Task<PaymentAccountResponse?> GetByIdAsync(int id, int applicationId);
    Task<PaymentAccountResponse> CreateAsync(PaymentAccountCreateRequest request, int applicationId);
    Task<bool> UpdateAsync(int id, PaymentAccountUpdateRequest request, int applicationId);
    Task<bool> DeleteAsync(int id, int applicationId);
    Task<byte[]> ExportListAsync(PaymentAccountSearchRequest request, int applicationId, ListExportFormat format);
}
