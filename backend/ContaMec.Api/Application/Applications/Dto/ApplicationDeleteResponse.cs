namespace ContaMec.Api.Application.Applications.Dto;

/// <summary>Resultado de eliminar una aplicación: cuántos registros se borraron de cada tabla.</summary>
public class ApplicationDeleteResponse
{
    /// <summary>Tablas con registros borrados, en el orden en que se borraron; la aplicación va al final.</summary>
    public List<ApplicationDeletedTable> Deleted { get; set; } = new();

    /// <summary>Cotizaciones que cargaron sus usuarios: son compartidas, no se borran, solo quedan sin usuario.</summary>
    public int UnlinkedExchangeRates { get; set; }

    public int Total => Deleted.Sum(d => d.Count);
}

public class ApplicationDeletedTable
{
    /// <summary>Nombre de la tabla en la base, por ejemplo "Incomes".</summary>
    public string Table { get; set; } = string.Empty;

    /// <summary>Nombre para mostrar, por ejemplo "Ingresos".</summary>
    public string Label { get; set; } = string.Empty;

    public int Count { get; set; }
}
