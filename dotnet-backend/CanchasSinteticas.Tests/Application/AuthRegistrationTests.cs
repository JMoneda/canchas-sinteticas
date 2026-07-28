using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Application.Services;
using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Exceptions;
using CanchasSinteticas.Infrastructure.Persistence;
using CanchasSinteticas.Infrastructure.Repositories;
using CanchasSinteticas.Tests.Support;
using Xunit;

namespace CanchasSinteticas.Tests.Application;

/// <summary>
/// Verifica el endurecimiento de la validación de registro: la política de contraseña
/// (mínimo 8, letras + números) y el formato de correo se aplican en el backend.
/// </summary>
public class AuthRegistrationTests
{
    private sealed class NoopPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";
        public bool Verify(string password, string hash) => hash == $"hash:{password}";
    }

    private sealed class StubTokenService : ITokenService
    {
        public string CreateToken(User user) => $"token:{user.Id}";
    }

    private static AuthService BuildService()
    {
        var db = new InMemoryDatabase();
        var users = new InMemoryUserRepository(db);
        return new AuthService(users, new NoopPasswordHasher(), new StubTokenService(),
            new FixedClock(new DateTime(2026, 7, 24, 12, 0, 0)));
    }

    private static RegisterInput NewInput(string password = "Futbol2026", string email = "nuevo@canchas.co") =>
        new("Juan Pérez", email, null, password, "Client");

    [Fact]
    public void Register_rejects_weak_numeric_password()
    {
        var service = BuildService();
        var ex = Assert.Throws<ValidationError>(() => service.Register(NewInput(password: "123456")));
        Assert.Contains("8 caracteres", ex.Message);
    }

    [Fact]
    public void Register_rejects_password_without_number()
    {
        var service = BuildService();
        Assert.Throws<ValidationError>(() => service.Register(NewInput(password: "abcdefgh")));
    }

    [Fact]
    public void Register_rejects_password_without_letter()
    {
        var service = BuildService();
        Assert.Throws<ValidationError>(() => service.Register(NewInput(password: "12345678")));
    }

    [Theory]
    [InlineData("juan@")]
    [InlineData("juan@dominio")]
    [InlineData("sinarroba.com")]
    [InlineData("juan @canchas.co")]
    public void Register_rejects_invalid_email(string email)
    {
        var service = BuildService();
        Assert.Throws<ValidationError>(() => service.Register(NewInput(email: email)));
    }

    [Fact]
    public void Register_accepts_valid_input()
    {
        var service = BuildService();
        var result = service.Register(NewInput());
        Assert.Equal("Juan Pérez", result.Name);
        Assert.Equal("nuevo@canchas.co", result.Email);
        Assert.False(string.IsNullOrEmpty(result.Token));
    }

    [Theory]
    [InlineData("32222222222")]   // 11 dígitos
    [InlineData("123")]            // muy corto
    [InlineData("3333333333")]     // dígito repetido (basura)
    [InlineData("1001234567")]     // no es celular (3XX) ni fijo (60X)
    public void Register_rejects_invalid_phone(string phone)
    {
        var service = BuildService();
        Assert.Throws<ValidationError>(
            () => service.Register(new RegisterInput("Juan Pérez", "tel@canchas.co", phone, "Futbol2026", "Client")));
    }

    [Theory]
    [InlineData("3001234567")]        // celular
    [InlineData("+57 300 123 4567")]  // con indicativo
    [InlineData("604 123 4567")]      // fijo
    public void Register_accepts_valid_phone(string phone)
    {
        var service = BuildService();
        var result = service.Register(new RegisterInput("Juan Pérez", "tel@canchas.co", phone, "Futbol2026", "Client"));
        Assert.False(string.IsNullOrEmpty(result.Token));
    }
}
