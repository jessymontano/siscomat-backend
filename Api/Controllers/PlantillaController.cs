using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siscomat.Core.Interfaces;
using Siscomat.Services;

namespace Siscomat.Api.Controllers
{
    /// <summary>
    /// Controlador para manejar las operaciones relacionadas con las plantillas de constancias. Proporciona endpoints para listar todas las plantillas, obtener el archivo de una plantilla específica, subir nuevas plantillas y eliminar plantillas existentes. Utiliza el servicio PlantillaService para realizar la lógica de negocio y manejar la comunicación con la base de datos. Todos los endpoints requieren autenticación para garantizar que solo los gestores autorizados puedan gestionar las plantillas.
    /// </summary>
    [ApiController]
    [Route("api/plantillas")]
    [Authorize]
    public class PlantillaController(IPlantillaService plantillaService) : ControllerBase
    {
        private readonly IPlantillaService _plantillaService = plantillaService;

        /// <summary>
        /// Obtiene una lista de todas las plantillas disponibles en el sistema, incluyendo información básica como el ID, nombre, fecha de creación y si la plantilla ha sido utilizada para generar constancias. Este endpoint permite a los gestores ver rápidamente qué plantillas tienen disponibles y cuáles ya han sido usadas, lo que puede ayudar en la gestión y organización de las plantillas.
        /// </summary>
        /// <returns>Lista de plantillas con información básica.</returns>
        /// <response code="200">Lista de plantillas obtenida exitosamente.</response>
        /// <response code="401">No autorizado. El usuario no ha iniciado sesión o no tiene permisos para acceder a este recurso.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll()
        {
            var plantillas = await _plantillaService.GetAllAsync();
            return Ok(plantillas.Select(p => new
            {
                id = p.Id,
                nombre = p.Nombre,
                created_at = p.CreatedAt,
                en_uso = p.Constancias.Any()
            }));
        }

        /// <summary>
        /// Obtiene el archivo PDF de una plantilla específica utilizando su ID. Este endpoint es útil para que los gestores puedan descargar o previsualizar la plantilla antes de usarla para generar constancias. Si la plantilla no existe, se devuelve un error 404 indicando que no se encontró una plantilla con ese ID.
        /// </summary>
        /// <param name="id">ID de la plantilla a obtener.</param>
        /// <returns>Archivo PDF de la plantilla.</returns>
        /// <response code="200">Archivo PDF de la plantilla obtenido exitosamente.</response>
        /// <response code="404">No se encontró una plantilla con el ID proporcionado.</response>
        /// <response code="401">No autorizado. El usuario no ha iniciado sesión o no tiene permisos para acceder a este recurso.</response>
        [HttpGet("{id}/archivo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetArchivo(int id)
        {
            var resultado = await _plantillaService.GetArchivoAsync(id);
            if (resultado == null)
                return NotFound(new { error = "No existe una plantilla con ese id." });

            return File(resultado.Value.bytes, "application/pdf");
        }

        /// <summary>
        /// Permite subir una nueva plantilla al sistema. El gestor debe proporcionar un nombre para la plantilla y un archivo PDF que contenga los placeholders requeridos. El sistema validará que el archivo sea un PDF y que contenga los placeholders necesarios antes de guardarlo. Si la plantilla es válida, se guardará en el sistema y se devolverá una respuesta con la información de la plantilla creada. Si la plantilla no es válida, se devolverá un error indicando qué placeholders faltan o si el archivo no es un PDF.
        /// </summary>
        /// <param name="nombre">Nombre de la plantilla a subir.</param>
        /// <param name="archivo">Archivo PDF de la plantilla.</param>
        /// <returns>Información de la plantilla creada.</returns>
        /// <response code="201">Plantilla creada exitosamente.</response>
        /// <response code="400">Solicitud inválida. Faltan datos requeridos o el formato es incorrecto.</response>
        /// <response code="401">No autorizado. El usuario no ha iniciado sesión o no tiene permisos para acceder a este recurso.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Subir([FromForm] string nombre, IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest(new { error = "El archivo es requerido." });

            if (!archivo.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "El archivo debe ser un PDF." });

            try
            {
                var plantilla = await _plantillaService.SubirAsync(nombre, archivo);

                return CreatedAtAction(nameof(GetAll), new
                {
                    id = plantilla.Id,
                    nombre = plantilla.Nombre,
                    created_at = plantilla.CreatedAt,
                    en_uso = false
                });
            }
            catch (PlantillaInvalidaException ex)
            {
                return BadRequest(new 
                { 
                    error = "La plantilla no cuenta con los placeholders requeridos.",
                    detalles = ex.Detalles 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Elimina una plantilla existente utilizando su ID. Este endpoint permite a los gestores eliminar plantillas que ya no necesitan o que fueron subidas por error. Sin embargo, si la plantilla ya ha sido utilizada para generar constancias, el sistema no permitirá eliminarla y devolverá un error indicando que la plantilla está en uso. Si la plantilla se elimina exitosamente, se devolverá un mensaje de confirmación.
        /// </summary>
        /// <param name="id">ID de la plantilla a eliminar.</param>
        /// <returns>Resultado de la operación de eliminación.</returns>
        /// <response code="200">Plantilla eliminada exitosamente.</response>
        /// <response code="404">No encontrado. No existe una plantilla con el ID proporcionado.</response>
        /// <response code="409">Conflicto. La plantilla ya fue usada para generar constancias y no puede eliminarse.</response>
        /// <response code="401">No autorizado. El usuario no ha iniciado sesión o no tiene permisos para acceder a este recurso.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Eliminar(int id)
        {
            var (eliminado, enUso) = await _plantillaService.EliminarAsync(id);

            if (!eliminado)
                return NotFound(new { error = "No existe una plantilla con ese id." });

            if (enUso)
                return Conflict(new { error = "La plantilla ya fue usada para generar constancias y no puede eliminarse." });

            return Ok(new { mensaje = "Plantilla eliminada exitosamente." });
        }
    }
}