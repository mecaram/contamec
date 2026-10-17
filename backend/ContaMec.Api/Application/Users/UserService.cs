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

    public async Task<List<UserResponse>> SearchAsync(UserSearchRequest request, int applicationId)
    {
        request ??= new UserSearchRequest();

        var query = _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserRole)
            .Where(u => u.ApplicationId == applicationId);

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

        return await query
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Id)
            .Select(MapToResponse())
            .ToListAsync();
    }

    public async Task<UserResponse?> GetByIdAsync(int id, int applicationId)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserRole)
            .Where(u => u.Id == id && u.ApplicationId == applicationId)
            .Select(MapToResponse())
            .FirstOrDefaultAsync();
    }

    public async Task<UserResponse> CreateAsync(UserCreateRequest request, int applicationId)
    {
        ArgumentNullException.ThrowIfNull(request);
        await ValidateUserAsync(request.Name, request.Password, request.UserRoleId, null);

        var user = new User
        {
            Name = request.Name!.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password!),
            IsActive = request.IsActive,
            UserRoleId = request.UserRoleId!.Value,
            ApplicationId = applicationId
        };

        _dbContext.Users.Add(user);
        await SaveChangesCheckingDuplicateNameAsync();

        var created = await GetByIdAsync(user.Id, applicationId);
        return created ?? throw new InvalidOperationException("No se pudo leer el usuario recién creado.");
    }

    public async Task<bool> UpdateAsync(int id, UserUpdateRequest request, int applicationId)
    {
        ArgumentNullException.ThrowIfNull(request);
        await ValidateUserAsync(request.Name, request.Password, request.UserRoleId, id);

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id && u.ApplicationId == applicationId);
        if (user is null)
            return false;

        user.Name = request.Name!.Trim();
        user.IsActive = request.IsActive;
        user.UserRoleId = request.UserRoleId!.Value;

        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password.Trim());

        await SaveChangesCheckingDuplicateNameAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int applicationId)
    {
        var user = await _dbContext.Users
            .Include(u => u.IncomesCreated)
            .Include(u => u.ExpensesCreated)
            .FirstOrDefaultAsync(u => u.Id == id && u.ApplicationId == applicationId);
        if (user is null)
            return false;

        if ((user.IncomesCreated?.Count ?? 0) > 0 || (user.ExpensesCreated?.Count ?? 0) > 0)
            throw new ArgumentException("No se puede eliminar el usuario porque tiene movimientos asociados.");

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    private async Task SaveChangesCheckingDuplicateNameAsync()
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

    private static Expression<Func<User, UserResponse>> MapToResponse()
    {
        return user => new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            IsActive = user.IsActive,
            UserRoleId = user.UserRoleId,
            UserRoleName = user.UserRole != null ? user.UserRole.Name : null
        };
    }

    private async Task ValidateUserAsync(string? name, string? password, int? userRoleId, int? currentUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name es obligatorio.");
        if (name.Trim().Length > 80)
            throw new ArgumentException("Name no puede superar 80 caracteres.");

        if (currentUserId is null && string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password es obligatorio.");
        if (!string.IsNullOrWhiteSpace(password) && password.Trim().Length < 4)
            throw new ArgumentException("Password debe tener al menos 4 caracteres.");

        var normalizedName = name.Trim();
        var exists = await _dbContext.Users
            .AnyAsync(u => u.Name.ToLower() == normalizedName.ToLower() && (!currentUserId.HasValue || u.Id != currentUserId.Value));
        if (exists)
            throw new ArgumentException("Ya existe un usuario con ese nombre.");

        if (!userRoleId.HasValue)
            throw new ArgumentException("UserRoleId es obligatorio.");

        var roleExists = await _dbContext.UserRoles.AnyAsync(r => r.Id == userRoleId.Value);
        if (!roleExists)
            throw new ArgumentException("El rol seleccionado no existe.");
    }
}
