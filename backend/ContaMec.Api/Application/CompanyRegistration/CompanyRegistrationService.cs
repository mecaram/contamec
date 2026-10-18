using ContaMec.Api.Application.Applications;
using ContaMec.Api.Application.Applications.Dto;
using ContaMec.Api.Application.Auth;
using ContaMec.Api.Application.CompanyRegistration.Dto;
using ContaMec.Api.Application.Users;
using ContaMec.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContaMec.Api.Application.CompanyRegistration;

/// <summary>
/// Registro público: empresa, cierre inicial y usuario administrador, todos inactivos hasta que un SuperAdministrador los habilite.
/// Reutiliza las altas de <see cref="IApplicationService"/> e <see cref="IUserService"/> (mismas validaciones, cierre y hash).
/// </summary>
public class CompanyRegistrationService(
    ContaMecDbContext dbContext,
    IApplicationService applicationService,
    IUserService userService) : ICompanyRegistrationService
{
    private readonly ContaMecDbContext _dbContext = dbContext;

    public async Task RegisterAsync(CompanyRegistrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var application = await applicationService.AddAsync(new ApplicationCreateRequest
        {
            Name = request.Name,
            Active = false,
            WhatsApp = request.WhatsApp,
            Email = request.Email,
            Address = request.Address,
            Locality = request.Locality
        });

        var administratorRoleId = await _dbContext.UserRoles
            .Where(r => r.Name == UserRoleNames.Administrator)
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException($"No existe el rol '{UserRoleNames.Administrator}'.");

        if (!string.IsNullOrEmpty(request.Password) && request.Password != request.ConfirmPassword)
            throw new ArgumentException("La contraseña y su repetición no coinciden.");

        var user = await userService.AddAsync(request.Username, request.Password, false, administratorRoleId, isSuperAdministrator: false);
        user.Application = application;

        // Un único SaveChanges corre en una transacción: si falla cualquier insert no queda empresa, cierre ni usuario sueltos.
        await userService.SaveChangesCheckingDuplicateNameAsync();
    }
}
