using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siscomat.Core.DTOs;
using Siscomat.Services;
using System.Security.Claims;

namespace Siscomat.Api.Controllers
{
    /// <summary>
    /// Controlador para manejar la autenticación de usuarios (gestores). Proporciona endpoints para iniciar sesión, cerrar sesión y verificar el estado de autenticación. Utiliza cookies para mantener la sesión del usuario y claims para almacenar información relevante del usuario autenticado.
    /// </summary>
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        protected readonly AuthService _authService;

        public AuthController(AuthService loginService)
        {
            _authService = loginService;
        }

        /// <summary>
        /// Inicia sesión para un gestor utilizando su correo y contraseña. Si las credenciales son correctas, se crea una cookie de autenticación con los claims del usuario y se devuelve un mensaje de éxito junto con la información básica del usuario.
        /// </summary>
        /// <param name="loginDto">Objeto que contiene el correo y la contraseña del gestor.</param>
        /// <returns>Resultado de la operación de inicio de sesión.</returns>
        /// <response code="200">Inicio de sesión exitoso.</response>
        /// <response code="401">Correo o contraseña incorrectos.</response>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            var gestor = await _authService.LoginAsync(loginDto);
            if (gestor == null)
            {
                return Unauthorized(new { message = "Correo o contraseña incorrectos" });
            }
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, gestor.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{gestor.Nombre} {gestor.Apellido1} {gestor.Apellido2 ?? ""}"),
                new Claim(ClaimTypes.Email, gestor.Correo),
                new Claim(ClaimTypes.Role, gestor.EsAdmin ? "Admin" : "Gestor")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return Ok(new
            {
                message = "Inicio de sesión exitoso",
                user = new
                {
                    id = gestor.Id,
                    nombre = gestor.Nombre,
                    esAdmin = gestor.EsAdmin
                }
            });
        }

        /// <summary>
        /// Cierra la sesión del usuario actual eliminando la cookie de autenticación.
        /// </summary>
        /// <returns>Resultado de la operación de cierre de sesión.</returns>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Sesión cerrada exitosamente" });
        }

        [HttpGet("access-denied")]
        public IActionResult AccessDenied()
        {
            return Unauthorized(new { message = "Acceso denegado" });
        }

        /// <summary>
        /// Verifica si el usuario actual está autenticado y devuelve un mensaje de bienvenida con su nombre.
        /// </summary>
        /// <remarks>
        /// Requiere que el usuario esté autenticado.
        /// </remarks>
        /// <response code="200">Usuario autenticado, devuelve mensaje de bienvenida.</response>
        /// <response code="401">Usuario no autenticado, acceso denegado.</response
        [Authorize]
        [HttpGet("logged")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Logged()
        {
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            return Ok($"Bienvenido, {userName}");
        }

        
    }
}
