using ContaMec.Api.Application.Plans.Dto;

namespace ContaMec.Api.Application.Plans;

public interface IPlanService
{
    Task<List<PlanResponse>> SearchAsync(PlanSearchRequest request);
    Task<List<string>> GetLocalitiesAsync();
    Task<PlanResponse?> ActivateDemoAsync(int applicationId);
    Task<PlanResponse?> ActivateProPlusAsync(int applicationId);
    Task<List<PlanHistoryResponse>> GetHistoryAsync(int applicationId);
    Task<PlanResponse?> DeleteLastAsync(int applicationId);
}
