namespace ContaMec.Api.Application.Orders;

/// <summary>Importe en letras, como en los comprobantes argentinos: "Son pesos mil doscientos con 50/100".</summary>
public static class AmountInWords
{
    private static readonly string[] Units =
    [
        "cero", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve",
        "diez", "once", "doce", "trece", "catorce", "quince", "dieciséis", "diecisiete", "dieciocho", "diecinueve",
        "veinte", "veintiuno", "veintidós", "veintitrés", "veinticuatro", "veinticinco", "veintiséis", "veintisiete", "veintiocho", "veintinueve"
    ];

    private static readonly string[] Tens = ["", "", "", "treinta", "cuarenta", "cincuenta", "sesenta", "setenta", "ochenta", "noventa"];

    private static readonly string[] Hundreds =
    [
        "", "ciento", "doscientos", "trescientos", "cuatrocientos", "quinientos", "seiscientos", "setecientos", "ochocientos", "novecientos"
    ];

    public static string ToPesos(decimal amount)
    {
        var rounded = Math.Round(Math.Abs(amount), 2, MidpointRounding.AwayFromZero);
        var integer = (long)Math.Truncate(rounded);
        var cents = (int)((rounded - integer) * 100);
        return $"Son pesos {Convert(integer, apocope: false)} con {cents:00}/100.";
    }

    // Con apócope, "uno" pasa a "un" delante de mil y millón: "veintiún mil", "un millón".
    private static string Convert(long number, bool apocope)
    {
        if (number >= 1_000_000)
        {
            var millions = number / 1_000_000;
            var rest = number % 1_000_000;
            var head = millions == 1 ? "un millón" : $"{Convert(millions, apocope: true)} millones";
            return rest == 0 ? head : $"{head} {Convert(rest, apocope)}";
        }

        if (number >= 1000)
        {
            var thousands = number / 1000;
            var rest = number % 1000;
            var head = thousands == 1 ? "mil" : $"{Convert(thousands, apocope: true)} mil";
            return rest == 0 ? head : $"{head} {Convert(rest, apocope)}";
        }

        if (number >= 100)
        {
            if (number == 100)
                return "cien";
            var rest = number % 100;
            var head = Hundreds[number / 100];
            return rest == 0 ? head : $"{head} {Convert(rest, apocope)}";
        }

        string words;
        if (number < 30)
            words = Units[number];
        else
        {
            var unit = number % 10;
            words = unit == 0 ? Tens[number / 10] : $"{Tens[number / 10]} y {Units[unit]}";
        }

        if (apocope && words.EndsWith("uno", StringComparison.Ordinal))
            words = words == "veintiuno" ? "veintiún" : words[..^1];
        return words;
    }
}
