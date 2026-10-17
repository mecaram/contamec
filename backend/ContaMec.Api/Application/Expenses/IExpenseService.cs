using ContaMec.Api.Application.Expenses.Dto;

namespace ContaMec.Api.Application.Expenses;

public interface IExpenseService
{
    Task<List<ExpenseResponse>> SearchAsync(ExpenseSearchRequest request, int applicationId);
    Task<ExpenseResponse?> GetByIdAsync(int id, int applicationId);
    Task<ExpenseResponse> CreateAsync(ExpenseCreateRequest request, int createdByUserId, int applicationId);
    Task<bool> UpdateAsync(int id, ExpenseUpdateRequest request, int applicationId);
    Task<bool> DeleteAsync(int id, int applicationId);
}
