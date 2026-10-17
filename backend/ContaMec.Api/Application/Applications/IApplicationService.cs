using ContaMec.Api.Application.Applications.Dto;

namespace ContaMec.Api.Application.Applications;

public interface IApplicationService
{
    Task<List<ApplicationResponse>> SearchAsync(ApplicationSearchRequest request);
    Task<ApplicationResponse?> GetByIdAsync(int id);
    Task<ApplicationResponse> CreateAsync(ApplicationCreateRequest request);
    Task<bool> UpdateAsync(int id, ApplicationUpdateRequest request, int currentApplicationId);
    Task<bool> DeleteAsync(int id, int currentApplicationId);
}
