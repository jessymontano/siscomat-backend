using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siscomat.Services;

namespace Siscomat.Api.Controllers
{
    /// <summary>
    /// Controlador para manejar las operaciones relacionadas con los gestores. Proporciona endpoints para crear, leer, actualizar y eliminar gestores. Utiliza el servicio GestorService para realizar la lógica de negocio y manejar la comunicación con la base de datos. Todos los endpoints requieren autenticación para garantizar que solo usuarios autorizados puedan acceder a la información de los gestores.
    /// </summary>
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

        /// <summary>
        /// Obtiene una lista de todos los gestores registrados en el sistema. Devuelve un objeto con la información básica de cada gestor, incluyendo su id, nombre, apellidos, correo y si es administrador. Este endpoint requiere autenticación para garantizar que solo usuarios autorizados puedan acceder a la información de los gestores.
        /// </summary>
        /// <returns>Lista de gestores con información básica.</returns>
        /// <response code="200">Lista de gestores obtenida exitosamente.</response>
        /// <response code="401">No autorizado. El usuario no ha iniciado sesión o no tiene permisos para acceder a este recurso.</response>
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

        /// <summary>
        /// Obtiene la información de un gestor específico por su id. Devuelve un objeto con la información básica del gestor, incluyendo su id, nombre, apellidos, correo y si es administrador. Este endpoint requiere autenticación para garantizar que solo usuarios autorizados puedan acceder a la información de los gestores.
        /// </summary>
        /// <param name="id">ID del gestor a obtener.</param>
        /// <returns>Información del gestor solicitado.</returns>
        /// <response code="200">Gestor encontrado y devuelto exitosamente.</response>
        /// <response code="404">No se encontró un gestor con el ID proporcionado.</response>
        /// <response code="401">No autorizado. El usuario no ha iniciado sesión o no tiene permisos para acceder a este recurso.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

        /// <summary>
        /// Permite crear un nuevo gestor en el sistema. Requiere que se proporcione un objeto con la información del gestor a crear, incluyendo su nombre, apellidos, correo, contraseña y si es administrador. El correo debe ser único en el sistema y la contraseña es obligatoria. Este endpoint requiere autenticación para garantizar que solo usuarios autorizados puedan crear nuevos gestores.
        /// </summary>
        /// <param name="gestor">Objeto que contiene la información del gestor a crear.</param>
        /// <returns>Información del gestor creado.</returns>
        /// <response code="201">Gestor creado exitosamente.</response>
        /// <response code="400">Solicitud inválida. Faltan datos requeridos o el formato es incorrecto.</response>
        /// <response code="409">Conflicto. Ya existe un gestor registrado con el mismo correo.</response>
        /// <response code="401">No autorizado. El usuario no ha iniciado sesión o no tiene permisos para acceder a este recurso.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

        /// <summary>
        /// Actualiza la información de un gestor existente. Requiere que se proporcione el ID del gestor a actualizar y un objeto con la nueva información del gestor, incluyendo su nombre, apellidos, correo, contraseña y si es administrador. El correo debe ser único en el sistema y la contraseña es opcional. Si se proporciona una nueva contraseña, esta será actualizada; de lo contrario, se mantendrá la contraseña actual. Este endpoint requiere autenticación para garantizar que solo usuarios autorizados puedan actualizar la información de los gestores.
        /// </summary>
        /// <param name="id">ID del gestor a actualizar.</param>
        /// <param name="gestor">Objeto que contiene la nueva información del gestor.</param>
        /// <returns>Información del gestor actualizado.</returns>
        /// <response code="200">Gestor actualizado exitosamente.</response>
        /// <response code="400">Solicitud inválida. Faltan datos requeridos o el formato es incorrecto.</response>
        /// <response code="404">No encontrado. No existe un gestor con el ID proporcionado.</response>
        /// <response code="409">Conflicto. Ya existe un gestor registrado con el mismo correo.</response>
        /// <response code="401">No autorizado. El usuario no ha iniciado sesión o no tiene permisos para acceder a este recurso.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

        /// <summary>
        /// Elimina un gestor del sistema. Requiere que se proporcione el ID del gestor a eliminar. Si el gestor existe, será eliminado de la base de datos. Este endpoint requiere autenticación para garantizar que solo usuarios autorizados puedan eliminar gestores.
        /// </summary>
        /// <param name="id">ID del gestor a eliminar.</param>
        /// <returns>Mensaje de confirmación de eliminación.</returns>
        /// <response code="200">Gestor eliminado exitosamente.</response>
        /// <response code="404">No encontrado. No existe un gestor con el ID proporcionado.</response>
        /// <response code="401">No autorizado. El usuario no ha iniciado sesión o no tiene permisos para acceder a este recurso.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
