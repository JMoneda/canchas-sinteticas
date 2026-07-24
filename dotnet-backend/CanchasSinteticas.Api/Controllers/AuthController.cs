using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanchasSinteticas.Api.Controllers;

/// <summary>Registro, inicio de sesión y perfil del usuario autenticado.</summary>
[Route("api/auth")]
public class AuthController(AuthService auth) : ApiControllerBase
{
    /// <summary>Registra una cuenta nueva (rol Owner o Client) y devuelve un token.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthOutput), 200)]
    public IActionResult Register([FromBody] RegisterInput input) => Ok(auth.Register(input));

    /// <summary>Inicia sesión con correo y contraseña.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthOutput), 200)]
    public IActionResult Login([FromBody] LoginInput input) => Ok(auth.Login(input));

    /// <summary>Devuelve el perfil del usuario autenticado.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserOutput), 200)]
    public IActionResult Me() => Ok(auth.Me(CurrentUserId));
}
