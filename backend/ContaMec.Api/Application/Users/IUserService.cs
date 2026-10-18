using ContaMec.Api.Application.Users.Dto;
using ContaMec.Api.Domain.Entities;

namespace ContaMec.Api.Application.Users;

public interface IUserService
{
    Task<List<UserResponse>> SearchAsync(UserSearchRequest request, int applicationId, bool isSuperAdministrator);
    Task<UserResponse?> GetByIdAsync(int id, int applicationId, bool isSuperAdministrator);
    Task<UserResponse> CreateAsync(UserCreateRequest request, int applicationId, bool isSuperAdministrator);

    /// <summary>Valida y agrega el usuario al contexto sin empresa asignada y sin guardar: el llamador la asigna y confirma.</summary>
    Task<User> AddAsync(string? name, string? password, bool isActive, int? userRoleId, bool isSuperAdministrator);

    /// <summary>Guarda los cambios pendientes; un nombre de usuario duplicado por concurrencia se informa como ArgumentException.</summary>
    Task SaveChangesCheckingDuplicateNameAsync();
    Task<bool> UpdateAsync(int id, UserUpdateRequest request, int applicationId, bool isSuperAdministrator);
    Task<bool> DeleteAsync(int id, int applicationId, bool isSuperAdministrator);
}
