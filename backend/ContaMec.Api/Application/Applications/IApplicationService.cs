using ContaMec.Api.Application.Applications.Dto;
using ContaMec.Api.Application.Orders;
using ApplicationEntity = ContaMec.Api.Domain.Entities.Application;

namespace ContaMec.Api.Application.Applications;

public interface IApplicationService
{
    Task<List<ApplicationResponse>> SearchAsync(ApplicationSearchRequest request);
    Task<ApplicationResponse?> GetByIdAsync(int id);
    Task<ApplicationResponse> CreateAsync(ApplicationCreateRequest request);

    /// <summary>Valida y agrega la aplicación y su cierre inicial al contexto, sin guardar: el llamador confirma junto con sus propios cambios.</summary>
    Task<ApplicationEntity> AddAsync(ApplicationCreateRequest request);
    Task<bool> UpdateAsync(int id, ApplicationUpdateRequest request, int currentApplicationId);
    Task<bool> DeleteAsync(int id, int currentApplicationId);

    /// <summary>Activa una aplicación inactiva y todos sus usuarios, le inicia una demo de 7 días y avisa por correo a la empresa. Null si no existe.</summary>
    Task<ApplicationActivationResponse?> ActivateAsync(int id);
    Task<byte[]> ExportListAsync(ApplicationSearchRequest request, int? currentApplicationId, ListExportFormat format);
}
