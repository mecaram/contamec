using ContaMec.Api.Application.ClosureBalances.Dto;

namespace ContaMec.Api.Application.ClosureBalances;

public interface IClosureBalanceService
{
    Task<List<ClosureBalanceResponse>> GetByClosureIdAsync(int closureId);
    Task<List<ClosureBalanceResponse>> SaveBulkAsync(int closureId, List<ClosureBalanceSaveRequest> items);
}
