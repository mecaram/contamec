using ContaMec.Api.Application.Auth;
using ContaMec.Api.Application.Users.Dto;
using ContaMec.Api.Domain.Entities;
using ContaMec.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Linq.Expressions;

namespace ContaMec.Api.Application.Users;

public class UserService(ContaMecDbContext dbContext) : IUserService
{
    private readonly ContaMecDbContext _dbContext = dbContext;

    public async Task<List<UserResponse>> SearchAsync(UserSearchRequest request, int applicationId, bool isSuperAdministrator)
    {
        request ??= new UserSearchRequest();

        IQueryable<User> query = UsersInScope(applicationId, isSuperAdministrator)
            .AsNoTracking()
            .Include(u => u.UserRole);

        if (request.Id.HasValue)
            query = query.Where(u => u.Id == request.Id.Value);

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            query = query.Where(u => u.Name != null && u.Name.ToLower().Contains(name.ToLower()));
        }

        if (request.IsActive.HasValue)
            query = query.Where(u => u.IsActive == request.IsActive.Value);

        if (request.UserRoleId.HasValue)
            query = query.Where(u => u.UserRoleId == request.UserRoleId.Value);

        if (request.ApplicationId.HasValue)
            query = query.Where(u => u.ApplicationId == request.ApplicationId.Value);

        return await query
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Id)
            .Select(MapToResponse())
            .ToListAsync();
    }

    public async Task<UserResponse?> GetByIdAsync(int id, int applicationId, bool isSuperAdministrator)
    {
        return await UsersInScope(applicationId, isSuperAdministrator)
            .AsNoTracking()
            .Include(u => u.UserRole)
            .Where(u => u.Id == id)
            .Select(MapToResponse())
            .FirstOrDefaultAsync();
    }

    public async Task<UserResponse> CreateAsync(UserCreateRequest request, int applicationId, bool isSuperAdministrator)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await AddAsync(request.Name, request.Password, request.IsActive, request.UserRoleId, isSuperAdministrator);
        var targetApplicationId = await ResolveTargetApplicationIdAsync(request.ApplicationId, applicationId, isSuperAdministrator);
        user.ApplicationId = targetApplicationId;

        await SaveChangesCheckingDuplicateNameAsync();

        var created = await GetByIdAsync(user.Id, targetApplicationId, isSuperAdministrator);
        return created ?? throw new InvalidOperationException("No se pudo leer el usuario recién creado.");
    }

    public async Task<User> AddAsync(string? name, string? password, bool isActive, int? userRoleId, bool isSuperAdministrator)
    {
        await ValidateUserAsync(name, password, userRoleId, null, isSuperAdministrator);

        var user = new User
        {
            Name = name!.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password!),
            IsActive = isActive,
            CreatedAt = ArgentinaClock.Now,
            UserRoleId = userRoleId!.Value
        };

        _dbContext.Users.Add(user);
        return user;
    }

    public async Task<bool> UpdateAsync(int id, UserUpdateRequest request, int applicationId, bool isSuperAdministrator)
    {
        ArgumentNullException.ThrowIfNull(request);
        await ValidateUserAsync(request.Name, request.Password, request.UserRoleId, id, isSuperAdministrator);

        var user = await UsersInScope(applicationId, isSuperAdministrator)
            .Include(u => u.UserRole)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return false;

        EnsureCanManage(user, isSuperAdministrator);

        user.Name = request.Name!.Trim();
        user.IsActive = request.IsActive;
        user.UserRoleId = request.UserRoleId!.Value;

        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password.Trim());

        await SaveChangesCheckingDuplicateNameAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int applicationId, bool isSuperAdministrator)
    {
        var user = await UsersInScope(applicationId, isSuperAdministrator)
            .Include(u => u.UserRole)
            .Include(u => u.IncomesCreated)
            .Include(u => u.ExpensesCreated)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return false;

        EnsureCanManage(user, isSuperAdministrator);

        if ((user.IncomesCreated?.Count ?? 0) > 0 || (user.ExpensesCreated?.Count ?? 0) > 0)
            throw new ArgumentException("No se puede eliminar el usuario porque tiene movimientos asociados.");

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task SaveChangesCheckingDuplicateNameAsync()
    {
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "Users_Name_lower_key" })
        {
            throw new ArgumentException("Ya existe un usuario con ese nombre.");
        }
    }

    /// <summary>Un SuperAdministrador administra los usuarios de todas las empresas; el resto, solo los de la propia.</summary>
    private IQueryable<User> UsersInScope(int applicationId, bool isSuperAdministrator) =>
        isSuperAdministrator
            ? _dbContext.Users
            : _dbContext.Users.Where(u => u.ApplicationId == applicationId);

    private static Expression<Func<User, UserResponse>> MapToResponse()
    {
        return user => new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            IsActive = user.IsActive,
            UserRoleId = user.UserRoleId,
            UserRoleName = user.UserRole != null ? user.UserRole.Name : null,
            ApplicationId = user.ApplicationId,
            ApplicationName = user.Application != null ? user.Application.Name : null
        };
    }

    private async Task<int> ResolveTargetApplicationIdAsync(int? requestedApplicationId, int currentApplicationId, bool isSuperAdministrator)
    {
        if (!requestedApplicationId.HasValue || requestedApplicationId.Value == currentApplicationId)
            return currentApplicationId;

        if (!isSuperAdministrator)
            throw new ArgumentException("No tiene permisos para crear usuarios en otra empresa.");

        var applicationIsActive = await _dbContext.Applications
            .AnyAsync(a => a.Id == requestedApplicationId.Value && a.Active);
        if (!applicationIsActive)
            throw new ArgumentException("La empresa seleccionada no existe o no está activa.");

        return requestedApplicationId.Value;
    }

    private static void EnsureCanManage(User user, bool isSuperAdministrator)
    {
        if (!isSuperAdministrator && user.UserRole?.Name == UserRoleNames.SuperAdministrator)
            throw new ArgumentException("No tiene permisos para modificar o eliminar un usuario SuperAdministrador.");
    }

    private async Task ValidateUserAsync(string? name, string? password, int? userRoleId, int? currentUserId, bool isSuperAdministrator)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name es obligatorio.");
        if (name.Trim().Length > 80)
            throw new ArgumentException("Name no puede superar 80 caracteres.");
        if (name.Any(char.IsWhiteSpace))
            throw new ArgumentException("El nombre de usuario no puede contener espacios.");

        if (currentUserId is null && string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password es obligatorio.");
        if (!string.IsNullOrWhiteSpace(password))
            PasswordPolicy.Validate(password.Trim());

        var normalizedName = name.Trim();
        var exists = await _dbContext.Users
            .AnyAsync(u => u.Name.ToLower() == normalizedName.ToLower() && (!currentUserId.HasValue || u.Id != currentUserId.Value));
        if (exists)
            throw new ArgumentException("Ya existe un usuario con ese nombre.");

        if (!userRoleId.HasValue)
            throw new ArgumentException("UserRoleId es obligatorio.");

        var roleName = await _dbContext.UserRoles
            .Where(r => r.Id == userRoleId.Value)
            .Select(r => r.Name)
            .FirstOrDefaultAsync();
        if (roleName is null)
            throw new ArgumentException("El rol seleccionado no existe.");
        if (!isSuperAdministrator && roleName == UserRoleNames.SuperAdministrator)
            throw new ArgumentException("No tiene permisos para asignar el rol SuperAdministrador.");
    }
}
