namespace ContaMec.Api.Application.Incomes.Dto;

/// <summary>Valores que se proponen en el alta de un ingreso, tomados de los últimos ingresos que registró el usuario.</summary>
public class IncomeDefaultsResponse
{
    /// <summary>Cuenta del último ingreso registrado.</summary>
    public int? AccountId { get; set; }

    /// <summary>Subcuenta del último ingreso registrado, si la tenía.</summary>
    public int? SubAccountId { get; set; }

    /// <summary>Cuenta de pago del último ingreso que tenía una.</summary>
    public int? PaymentAccountId { get; set; }
}
