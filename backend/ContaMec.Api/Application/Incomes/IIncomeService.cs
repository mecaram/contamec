using ContaMec.Api.Application.Incomes.Dto;
using ContaMec.Api.Application.Orders;

namespace ContaMec.Api.Application.Incomes;

public interface IIncomeService
{
    Task<List<IncomeResponse>> SearchAsync(IncomeSearchRequest request, int applicationId);
    Task<IncomeResponse?> GetByIdAsync(int id, int applicationId);
    Task<IncomeResponse> CreateAsync(IncomeCreateRequest request, int createdByUserId, int applicationId);
    Task<IncomeDefaultsResponse> GetDefaultsAsync(int userId, int applicationId);
    Task<bool> UpdateAsync(int id, IncomeUpdateRequest request, int applicationId);
    Task<bool> DeleteAsync(int id, int applicationId);
    Task<byte[]?> GetOrderPdfAsync(int id, int applicationId);
    Task<byte[]> ExportListAsync(IncomeSearchRequest request, int applicationId, ListExportFormat format);
}
