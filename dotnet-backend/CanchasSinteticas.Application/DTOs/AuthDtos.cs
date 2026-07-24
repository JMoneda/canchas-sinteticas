namespace CanchasSinteticas.Application.DTOs;

/// <summary>Datos para registrar una cuenta nueva.</summary>
public record RegisterInput(
    string Name,
    string Email,
    string? Phone,
    string Password,
    string Role);

/// <summary>Credenciales de inicio de sesión.</summary>
public record LoginInput(string Email, string Password);

/// <summary>Resultado de una autenticación exitosa.</summary>
public record AuthOutput(
    string Token,
    string UserId,
    string Name,
    string Email,
    string Role);

/// <summary>Representación pública de un usuario.</summary>
public record UserOutput(
    string Id,
    string Name,
    string Email,
    string? Phone,
    string Role);
