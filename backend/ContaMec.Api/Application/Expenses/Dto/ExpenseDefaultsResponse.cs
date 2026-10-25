namespace ContaMec.Api.Application.Expenses.Dto;

/// <summary>Valores que se proponen en el alta de un egreso, tomados de los últimos egresos que registró el usuario.</summary>
public class ExpenseDefaultsResponse
{
    /// <summary>Cuenta del último egreso registrado.</summary>
    public int? AccountId { get; set; }

    /// <summary>Subcuenta del último egreso registrado, si la tenía.</summary>
    public int? SubAccountId { get; set; }

    /// <summary>Cuenta de pago del último egreso que tenía una.</summary>
    public int? PaymentAccountId { get; set; }
}
