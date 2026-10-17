namespace ContaMec.Api.Application.Auth;

/// <summary>Reglas de contraseña: mínimo 8 caracteres, al menos 2 números y 1 carácter especial (ni letra, ni número, ni espacio).</summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MinDigits = 2;

    public static void Validate(string password)
    {
        if (password.Length < MinLength)
            throw new ArgumentException($"La contraseña debe tener al menos {MinLength} caracteres.");
        if (password.Count(char.IsDigit) < MinDigits)
            throw new ArgumentException($"La contraseña debe tener al menos {MinDigits} números.");
        if (!password.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)))
            throw new ArgumentException("La contraseña debe tener al menos un carácter especial.");
    }
}
