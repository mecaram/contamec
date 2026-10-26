using ContaMec.Api.Application.Auth;
using ContaMec.Api.Application.Incomes;
using ContaMec.Api.Application.Incomes.Dto;
using ContaMec.Api.Application.Orders;
using ContaMec.Api.Application.Vouchers;
using ContaMec.Api.Application.Vouchers.Dto;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContaMec.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Tags("Ingresos")]
public class IncomesController(IIncomeService incomeService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<IncomeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Search(
        [FromQuery(Name = "dateFrom")] DateTime? dateFrom,
        [FromQuery(Name = "dateTo")] DateTime? dateTo,
        [FromQuery(Name = "closureId")] int? closureId,
        [FromQuery(Name = "accountId")] int? accountId,
        [FromQuery(Name = "paymentAccountId")] int? paymentAccountId,
        [FromQuery(Name = "subAccountId")] int? subAccountId,
        [FromQuery(Name = "amountFrom")] decimal? amountFrom,
        [FromQuery(Name = "amountTo")] decimal? amountTo,
        [FromQuery(Name = "detail")] string? detail)
    {
        var request = new IncomeSearchRequest
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            ClosureId = closureId,
            AccountId = accountId,
            PaymentAccountId = paymentAccountId,
            SubAccountId = subAccountId,
            AmountFrom = amountFrom,
            AmountTo = amountTo,
            Detail = detail
        };

        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        var result = await incomeService.SearchAsync(request, applicationId);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(IncomeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(int id)
    {
        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        var result = await incomeService.GetByIdAsync(id, applicationId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("last-payment-account")]
    [ProducesResponseType(typeof(IncomeDefaultsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDefaults()
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new { message = "Usuario no autenticado." });

        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        return Ok(await incomeService.GetDefaultsAsync(currentUserId, applicationId));
    }

    [HttpPost]
    [ProducesResponseType(typeof(IncomeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] IncomeCreateRequest? request)
    {
        if (request is null)
            return BadRequest("Cuerpo JSON requerido.");

        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new { message = "Usuario no autenticado." });

        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        try
        {
            var created = await incomeService.CreateAsync(request, currentUserId, applicationId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] IncomeUpdateRequest? request)
    {
        if (request is null)
            return BadRequest("Cuerpo JSON requerido.");

        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        try
        {
            var updated = await incomeService.UpdateAsync(id, request, applicationId);
            return updated ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id)
    {
        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        try
        {
            var deleted = await incomeService.DeleteAsync(id, applicationId);
            return deleted ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Margen sobre los 10 MB del archivo para el resto del cuerpo multipart.
    private const long VoucherRequestLimit = VoucherService.MaxBytes + 1024 * 1024;

    [HttpPost("{id:int}/voucher")]
    [RequestSizeLimit(VoucherRequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = VoucherRequestLimit)]
    [ProducesResponseType(typeof(VoucherResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> UploadVoucher(int id, IFormFile? file)
    {
        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Seleccione el archivo del comprobante." });
        if (file.Length > VoucherService.MaxBytes)
            return BadRequest(new { message = "El comprobante no puede superar los 10 MB." });

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer);

        try
        {
            var result = await incomeService.SaveVoucherAsync(id, applicationId, new VoucherFile(file.FileName, file.ContentType, buffer.ToArray()));
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (VoucherStorageException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}/voucher")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> RemoveVoucher(int id)
    {
        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        try
        {
            var removed = await incomeService.RemoveVoucherAsync(id, applicationId);
            return removed is null ? NotFound() : NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (VoucherStorageException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/voucher")]
    [ProducesResponseType(typeof(VoucherUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetVoucherUrl(int id)
    {
        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        try
        {
            var result = await incomeService.GetVoucherUrlAsync(id, applicationId);
            return result is null ? NotFound() : Ok(result);
        }
        catch (VoucherStorageException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }

    [HttpGet("export/{format}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportList(string format, [FromQuery] IncomeSearchRequest request)
    {
        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        var (exportFormat, contentType, extension) = format.ToLowerInvariant() switch
        {
            "pdf" => (ListExportFormat.Pdf, "application/pdf", "pdf"),
            "excel" => (ListExportFormat.Excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            _ => (default(ListExportFormat?), "", "")
        };
        if (exportFormat is null)
            return BadRequest(new { message = "Formato no soportado: use pdf o excel." });

        var file = await incomeService.ExportListAsync(request, applicationId, exportFormat.Value);
        return File(file, contentType, MovementListReport.FileName(OrderKind.Income, extension));
    }

    [HttpGet("{id:int}/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrderPdf(int id)
    {
        if (!User.TryGetApplicationId(out var applicationId))
            return Forbid();

        var pdf = await incomeService.GetOrderPdfAsync(id, applicationId);
        return pdf is null ? NotFound() : File(pdf, "application/pdf", OrderPdf.FileName(OrderKind.Income, id));
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var claimValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("nameid");

        return int.TryParse(claimValue, out userId);
    }
}
