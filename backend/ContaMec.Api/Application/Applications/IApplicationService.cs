using ContaMec.Api.Application.Applications.Dto;
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
}
