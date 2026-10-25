using ContaMec.Api.Application.Plans.Dto;

namespace ContaMec.Api.Application.Plans;

public interface IPlanService
{
    Task<List<PlanResponse>> SearchAsync(PlanSearchRequest request);
    Task<List<string>> GetLocalitiesAsync();
}
