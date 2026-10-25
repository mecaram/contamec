using ContaMec.Api.Application.Expenses.Dto;
using ContaMec.Api.Application.Orders;
using ContaMec.Api.Application.Vouchers;
using ContaMec.Api.Application.Vouchers.Dto;

namespace ContaMec.Api.Application.Expenses;

public interface IExpenseService
{
    Task<List<ExpenseResponse>> SearchAsync(ExpenseSearchRequest request, int applicationId);
    Task<ExpenseResponse?> GetByIdAsync(int id, int applicationId);
    Task<ExpenseResponse> CreateAsync(ExpenseCreateRequest request, int createdByUserId, int applicationId);
    Task<ExpenseDefaultsResponse> GetDefaultsAsync(int userId, int applicationId);
    Task<bool> UpdateAsync(int id, ExpenseUpdateRequest request, int applicationId);
    Task<bool> DeleteAsync(int id, int applicationId);
    /// <summary>Null si el egreso no existe; ArgumentException si el cierre está cerrado o el archivo no es válido.</summary>
    Task<VoucherResponse?> SaveVoucherAsync(int id, int applicationId, VoucherFile file);
    /// <summary>Null si el egreso no existe; false si no tenía comprobante.</summary>
    Task<bool?> RemoveVoucherAsync(int id, int applicationId);
    /// <summary>Null si el egreso no existe o no tiene comprobante.</summary>
    Task<VoucherUrlResponse?> GetVoucherUrlAsync(int id, int applicationId);
    Task<byte[]?> GetOrderPdfAsync(int id, int applicationId);
    Task<byte[]> ExportListAsync(ExpenseSearchRequest request, int applicationId, ListExportFormat format);
}
