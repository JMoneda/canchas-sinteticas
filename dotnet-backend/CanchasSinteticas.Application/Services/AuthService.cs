using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Application.Common;
using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Exceptions;
using CanchasSinteticas.Domain.Repositories;

namespace CanchasSinteticas.Application.Services;

/// <summary>
/// Casos de uso de autenticación: registro, inicio de sesión y perfil.
/// </summary>
public class AuthService(
    IUserRepository users,
    IPasswordHasher hasher,
    ITokenService tokens,
    IClock clock)
{
    /// <summary>Registra una nueva cuenta (dueño o cliente) y devuelve un token.</summary>
    public AuthOutput Register(RegisterInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new ValidationError("El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(input.Email))
            throw new ValidationError("El correo es obligatorio.");
        if (string.IsNullOrWhiteSpace(input.Password) || input.Password.Length < 6)
            throw new ValidationError("La contraseña debe tener al menos 6 caracteres.");

        var role = Parsing.ParseRegistrationRole(input.Role);
        var email = input.Email.Trim().ToLowerInvariant();

        if (users.GetByEmail(email) is not null)
            throw new EmailAlreadyExistsError();

        var user = new User(
            Guid.NewGuid().ToString(),
            input.Name.Trim(),
            email,
            input.Phone,
            hasher.Hash(input.Password),
            role,
            clock.Now);

        users.Add(user);
        return BuildAuth(user);
    }

    /// <summary>Autentica un usuario por correo y contraseña.</summary>
    public AuthOutput Login(LoginInput input)
    {
        var email = input.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var user = users.GetByEmail(email) ?? throw new InvalidCredentialsError();

        if (!hasher.Verify(input.Password ?? string.Empty, user.PasswordHash))
            throw new InvalidCredentialsError();

        return BuildAuth(user);
    }

    private AuthOutput BuildAuth(User user) =>
        new(tokens.CreateToken(user), user.Id, user.Name, user.Email, user.Role.ToString());

    /// <summary>Devuelve el perfil del usuario autenticado.</summary>
    public UserOutput Me(string userId)
    {
        var user = users.GetById(userId) ?? throw new NotFoundError();
        return Mappers.ToOutput(user);
    }
}
