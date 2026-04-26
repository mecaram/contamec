using ContaMec.Api.Application.Incomes.Dto;

namespace ContaMec.Api.Application.Incomes;

public interface IIncomeService
{
    Task<List<IncomeResponse>> SearchAsync(IncomeSearchRequest request);
    Task<IncomeResponse?> GetByIdAsync(int id);
    Task<IncomeResponse> CreateAsync(IncomeCreateRequest request, int createdByUserId);
    Task<bool> UpdateAsync(int id, IncomeUpdateRequest request);
    Task<bool> DeleteAsync(int id);
}
