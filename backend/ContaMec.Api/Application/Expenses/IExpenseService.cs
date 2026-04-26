using ContaMec.Api.Application.Expenses.Dto;

namespace ContaMec.Api.Application.Expenses;

public interface IExpenseService
{
    Task<List<ExpenseResponse>> SearchAsync(ExpenseSearchRequest request);
    Task<ExpenseResponse?> GetByIdAsync(int id);
    Task<ExpenseResponse> CreateAsync(ExpenseCreateRequest request, int createdByUserId);
    Task<bool> UpdateAsync(int id, ExpenseUpdateRequest request);
    Task<bool> DeleteAsync(int id);
}
