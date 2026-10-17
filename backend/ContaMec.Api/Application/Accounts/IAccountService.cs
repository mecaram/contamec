using ContaMec.Api.Application.Accounts.Dto;

namespace ContaMec.Api.Application.Accounts;

public interface IAccountService
{
    Task<List<AccountResponse>> SearchAsync(AccountSearchRequest request, int applicationId);
    Task<AccountResponse?> GetByIdAsync(int id, int applicationId);
    Task<AccountResponse> CreateAsync(AccountCreateRequest request, int applicationId);
    Task<bool> UpdateAsync(int id, AccountUpdateRequest request, int applicationId);
    Task<bool> DeleteAsync(int id, int applicationId);
}
