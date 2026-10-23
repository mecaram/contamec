using ContaMec.Api.Application.Auth;
using ContaMec.Api.Application.Orders;
using ContaMec.Api.Application.Users;
using ContaMec.Api.Application.Users.Dto;
using ContaMec.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaMec.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Tags("Usuarios")]
public class UsersController(IUserService userService, ContaMecDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Search(
        [FromQuery(Name = "id")] int? id,
        [FromQuery(Name = "name")] string? name,
        [FromQuery(Name = "isActive")] bool? isActive,
        [FromQuery(Name = "userRoleId")] int? userRoleId,
        [FromQuery(Name = "applicationId")] int? applicationIdFilter)
    {
        var request = new UserSearchRequest
        {
            Id = id,
            Name = name,
            IsActive = isActive,
            UserRoleId = userRoleId,
            ApplicationId = applicationIdFilter
        };

        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        var result = await userService.SearchAsync(request, applicationId, User.IsInRole(UserRoleNames.SuperAdministrator));
        return Ok(result);
    }

    [HttpGet("export/{format}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportList(
        string format,
        [FromQuery(Name = "id")] int? id,
        [FromQuery(Name = "name")] string? name,
        [FromQuery(Name = "isActive")] bool? isActive,
        [FromQuery(Name = "userRoleId")] int? userRoleId,
        [FromQuery(Name = "applicationId")] int? applicationIdFilter)
    {
        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        var (exportFormat, contentType, extension) = format.ToLowerInvariant() switch
        {
            "pdf" => (ListExportFormat.Pdf, "application/pdf", "pdf"),
            "excel" => (ListExportFormat.Excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            _ => (default(ListExportFormat?), "", "")
        };
        if (exportFormat is null)
            return BadRequest(new { message = "Formato no soportado: use pdf o excel." });

        var request = new UserSearchRequest
        {
            Id = id,
            Name = name,
            IsActive = isActive,
            UserRoleId = userRoleId,
            ApplicationId = applicationIdFilter
        };
        var file = await userService.ExportListAsync(request, applicationId, User.IsInRole(UserRoleNames.SuperAdministrator), exportFormat.Value);
        return File(file, contentType, UserListReport.FileName(extension));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(int id)
    {
        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        var result = await userService.GetByIdAsync(id, applicationId, User.IsInRole(UserRoleNames.SuperAdministrator));
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] UserCreateRequest? request)
    {
        if (request is null)
            return BadRequest("Cuerpo JSON requerido.");

        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        try
        {
            var created = await userService.CreateAsync(request, applicationId, User.IsInRole(UserRoleNames.SuperAdministrator));
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] UserUpdateRequest? request)
    {
        if (request is null)
            return BadRequest("Cuerpo JSON requerido.");

        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        try
        {
            var updated = await userService.UpdateAsync(id, request, applicationId, User.IsInRole(UserRoleNames.SuperAdministrator));
            return updated ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id)
    {
        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        try
        {
            var deleted = await userService.DeleteAsync(id, applicationId, User.IsInRole(UserRoleNames.SuperAdministrator));
            return deleted ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("role-options")]
    [ProducesResponseType(typeof(List<UserRoleOptionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoleOptions()
    {
        // Solo un SuperAdministrador puede ver (y asignar) su propio rol.
        var isSuperAdministrator = User.IsInRole(UserRoleNames.SuperAdministrator);
        var roles = await dbContext.UserRoles
            .AsNoTracking()
            .Where(r => isSuperAdministrator || r.Name != UserRoleNames.SuperAdministrator)
            .OrderBy(r => r.Name)
            .ThenBy(r => r.Id)
            .Select(r => new UserRoleOptionResponse
            {
                Id = r.Id,
                Name = r.Name
            })
            .ToListAsync();

        return Ok(roles);
    }

    public class UserRoleOptionResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
