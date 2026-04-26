using ContaMec.Api.Application.Closures.Dto;

namespace ContaMec.Api.Application.Closures;

public interface IClosureService
{
    Task<List<ClosureResponse>> SearchAsync(ClosureSearchRequest request);
    Task<ClosureIncomeDetailResponse?> GetIncomeDetailAsync(int closureId);
    Task<ClosureExpenseDetailResponse?> GetExpenseDetailAsync(int closureId);
    Task<ClosureCloseResponse> CloseAsync(int closureId);
}
