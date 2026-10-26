using ContaMec.Api.Application.Incomes.Dto;
using ContaMec.Api.Application.Orders;
using ContaMec.Api.Application.Vouchers;
using ContaMec.Api.Application.Vouchers.Dto;

namespace ContaMec.Api.Application.Incomes;

public interface IIncomeService
{
    Task<List<IncomeResponse>> SearchAsync(IncomeSearchRequest request, int applicationId);
    Task<IncomeResponse?> GetByIdAsync(int id, int applicationId);
    Task<IncomeResponse> CreateAsync(IncomeCreateRequest request, int createdByUserId, int applicationId);
    Task<IncomeDefaultsResponse> GetDefaultsAsync(int userId, int applicationId);
    Task<bool> UpdateAsync(int id, IncomeUpdateRequest request, int applicationId);
    Task<bool> DeleteAsync(int id, int applicationId);
    /// <summary>Null si el ingreso no existe; ArgumentException si el cierre está cerrado o el archivo no es válido.</summary>
    Task<VoucherResponse?> SaveVoucherAsync(int id, int applicationId, VoucherFile file);
    /// <summary>Null si el ingreso no existe; false si no tenía comprobante.</summary>
    Task<bool?> RemoveVoucherAsync(int id, int applicationId);
    /// <summary>Null si el ingreso no existe o no tiene comprobante.</summary>
    Task<VoucherUrlResponse?> GetVoucherUrlAsync(int id, int applicationId);
    Task<byte[]?> GetOrderPdfAsync(int id, int applicationId);
    Task<byte[]> ExportListAsync(IncomeSearchRequest request, int applicationId, ListExportFormat format);
}
