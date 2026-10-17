namespace ContaMec.Api.Application.Auth;

/// <summary>
/// Credenciales válidas, pero el inicio de sesión no se permite (p. ej. aplicación inactiva).
/// </summary>
public class LoginRejectedException(string message) : Exception(message);
