using ContaMec.Api.Application.Users.Dto;

namespace ContaMec.Api.Application.Users;

public interface IUserService
{
    Task<List<UserResponse>> SearchAsync(UserSearchRequest request, int applicationId);
    Task<UserResponse?> GetByIdAsync(int id, int applicationId);
    Task<UserResponse> CreateAsync(UserCreateRequest request, int applicationId);
    Task<bool> UpdateAsync(int id, UserUpdateRequest request, int applicationId);
    Task<bool> DeleteAsync(int id, int applicationId);
}
