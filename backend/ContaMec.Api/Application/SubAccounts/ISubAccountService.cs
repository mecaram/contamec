using ContaMec.Api.Application.Orders;
using ContaMec.Api.Application.SubAccounts.Dto;

namespace ContaMec.Api.Application.SubAccounts;

public interface ISubAccountService
{
    Task<List<SubAccountResponse>> SearchAsync(SubAccountSearchRequest request, int applicationId);
    Task<SubAccountResponse?> GetByIdAsync(int id, int applicationId);
    Task<SubAccountResponse> CreateAsync(SubAccountCreateRequest request, int applicationId);
    Task<bool> UpdateAsync(int id, SubAccountUpdateRequest request, int applicationId);
    Task<bool> DeleteAsync(int id, int applicationId);
    Task<byte[]> ExportListAsync(SubAccountSearchRequest request, int applicationId, ListExportFormat format);
}
