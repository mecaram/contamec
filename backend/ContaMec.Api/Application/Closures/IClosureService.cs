using ContaMec.Api.Application.Closures.Dto;

namespace ContaMec.Api.Application.Closures;

public interface IClosureService
{
    Task<List<ClosureResponse>> SearchAsync(ClosureSearchRequest request, int applicationId);
    Task<ClosureIncomeDetailResponse?> GetIncomeDetailAsync(int closureId, int applicationId);
    Task<ClosureExpenseDetailResponse?> GetExpenseDetailAsync(int closureId, int applicationId);
    Task<ClosureCloseResponse> CloseAsync(int closureId, int applicationId);
}
