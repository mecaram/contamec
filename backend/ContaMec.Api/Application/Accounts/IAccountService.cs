using ContaMec.Api.Application.Accounts.Dto;

namespace ContaMec.Api.Application.Accounts;

public interface IAccountService
{
    Task<List<AccountResponse>> SearchAsync(AccountSearchRequest request);
    Task<AccountResponse?> GetByIdAsync(int id);
    Task<AccountResponse> CreateAsync(AccountCreateRequest request);
    Task<bool> UpdateAsync(int id, AccountUpdateRequest request);
    Task<bool> DeleteAsync(int id);
}
