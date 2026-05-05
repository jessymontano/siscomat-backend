using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siscomat.Services;

namespace Siscomat.Api.Controllers
{
    [Route("/api/gestores")]
    [ApiController]
    [Authorize]
    public class GestorController : ControllerBase
    {
        protected readonly GestorService _gestorService;

        public GestorController(GestorService gestorService)
        {
            _gestorService = gestorService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var gestores = await _gestorService.GetAllAsync();
            return Ok(gestores.Select(g => new
            {
                id = g.Id,
                nombre = g.Nombre,
                apellido1 = g.Apellido1,
                apellido2 = g.Apellido2,
                correo = g.Correo,
                esAdmin = g.EsAdmin
            }));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var gestor = await _gestorService.GetByIdAsync(id);
            if (gestor == null)
                return NotFound(new { error = "No existe un gestor con ese id." });
            return Ok(new
            {
                id = gestor.Id,
                nombre = gestor.Nombre,
                apellido1 = gestor.Apellido1,
                apellido2 = gestor.Apellido2,
                correo = gestor.Correo,
                esAdmin = gestor.EsAdmin
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Core.Entities.Gestor gestor)
        {
            if (string.IsNullOrWhiteSpace(gestor.PasswordHash))
            {
                return BadRequest(new { error = "La contraseña es obligatoria." });
            }
            if (!isValidEmail(gestor.Correo))
            {
                return BadRequest(new { error = "El formato del correo es inválido." });
            }
            var existingCorreo = await _gestorService.GetByCorreoAsync(gestor.Correo);
            if (existingCorreo != null)
            {
                return Conflict(new { error = "Ya existe un gestor registrado con ese correo." });
            }
            await _gestorService.AddAsync(gestor);
            return CreatedAtAction(nameof(GetById), new { id = gestor.Id }, new
            {
                id = gestor.Id,
                nombre = gestor.Nombre,
                apellid_1 = gestor.Apellido1,
                apellido2 = gestor.Apellido2,
                correo = gestor.Correo,
                esAdmin = gestor.EsAdmin
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Core.Entities.Gestor gestor)
        {
            var existingGestor = await _gestorService.GetByIdAsync(id);
            if (existingGestor == null)
                return NotFound(new { error = "No existe un gestor con ese id." });
            if (!isValidEmail(gestor.Correo))
            {
                return BadRequest(new { error = "El formato del correo es inválido." });
            }
            var existingCorreo = await _gestorService.GetByCorreoAsync(gestor.Correo);
            if (existingCorreo != null && existingCorreo.Id != id)
            {
                return Conflict(new { error = "Ya existe un gestor registrado con ese correo." });
            }
            existingGestor.Nombre = gestor.Nombre;
            existingGestor.Apellido1 = gestor.Apellido1;
            existingGestor.Apellido2 = gestor.Apellido2;
            existingGestor.Correo = gestor.Correo;
            existingGestor.EsAdmin = gestor.EsAdmin;

            if (!string.IsNullOrWhiteSpace(gestor.PasswordHash))
            {
                existingGestor.PasswordHash = BCrypt.Net.BCrypt.HashPassword(gestor.PasswordHash);
            }

            await _gestorService.UpdateAsync(existingGestor);
            return Ok(new
            {
                message = "Datos de gestor actualizados correctamente.",
                id = existingGestor.Id,
                nombre = existingGestor.Nombre,
                apellido1 = existingGestor.Apellido1,
                apellido2 = existingGestor.Apellido2,
                correo = existingGestor.Correo,
                esAdmin = existingGestor.EsAdmin
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existingGestor = await _gestorService.GetByIdAsync(id);
            if (existingGestor == null)
                return NotFound(new { error = "No existe un gestor con ese id." });
            await _gestorService.DeleteAsync(id);
            return Ok(new { message = "Gestor eliminado correctamente." });
        }

        private bool isValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
