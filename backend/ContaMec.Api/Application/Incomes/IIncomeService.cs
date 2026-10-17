using ContaMec.Api.Application.Incomes.Dto;

namespace ContaMec.Api.Application.Incomes;

public interface IIncomeService
{
    Task<List<IncomeResponse>> SearchAsync(IncomeSearchRequest request, int applicationId);
    Task<IncomeResponse?> GetByIdAsync(int id, int applicationId);
    Task<IncomeResponse> CreateAsync(IncomeCreateRequest request, int createdByUserId, int applicationId);
    Task<bool> UpdateAsync(int id, IncomeUpdateRequest request, int applicationId);
    Task<bool> DeleteAsync(int id, int applicationId);
}
