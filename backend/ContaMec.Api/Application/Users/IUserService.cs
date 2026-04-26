using ContaMec.Api.Application.Users.Dto;

namespace ContaMec.Api.Application.Users;

public interface IUserService
{
    Task<List<UserResponse>> SearchAsync(UserSearchRequest request);
    Task<UserResponse?> GetByIdAsync(int id);
    Task<UserResponse> CreateAsync(UserCreateRequest request);
    Task<bool> UpdateAsync(int id, UserUpdateRequest request);
    Task<bool> DeleteAsync(int id);
}
