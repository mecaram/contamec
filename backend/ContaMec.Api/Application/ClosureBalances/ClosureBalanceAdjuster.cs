using ContaMec.Api.Domain.Entities;
using ContaMec.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContaMec.Api.Application.ClosureBalances;

/// <summary>Mantiene los valores en cuenta (ClosureBalances) de cada cuenta de pago en un cierre a medida que se cargan movimientos.</summary>
public static class ClosureBalanceAdjuster
{
    // Debe llamarse dentro de la transacción del movimiento. La suma la hace la base para no pisar cargas simultáneas.
    public static async Task AdjustAsync(ContaMecDbContext dbContext, int closureId, int? paymentAccountId, decimal delta, int applicationId)
    {
        if (!paymentAccountId.HasValue || delta == 0m)
            return;

        // Si hubiera más de una fila para la cuenta de pago, vale la primera, igual que en la pantalla de valores en cuenta.
        var balanceId = await dbContext.ClosureBalances
            .Where(b => b.ClosureId == closureId && b.PaymentAccountId == paymentAccountId.Value)
            .OrderBy(b => b.Id)
            .Select(b => (int?)b.Id)
            .FirstOrDefaultAsync();

        if (balanceId.HasValue)
        {
            await dbContext.ClosureBalances
                .Where(b => b.Id == balanceId.Value)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.Amount, b => (b.Amount ?? 0m) + delta));
            return;
        }

        dbContext.ClosureBalances.Add(new ClosureBalance
        {
            ClosureId = closureId,
            PaymentAccountId = paymentAccountId.Value,
            Amount = delta,
            ApplicationId = applicationId
        });
        await dbContext.SaveChangesAsync();
    }
}
