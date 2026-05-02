using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siscomat.Services;

namespace Siscomat.Api.Controllers
{
    [ApiController]
    [Route("api/plantillas")]
    [Authorize]
    public class PlantillaController : ControllerBase
    {
        private readonly PlantillaService _plantillaService;

        public PlantillaController(PlantillaService plantillaService)
        {
            _plantillaService = plantillaService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var plantillas = await _plantillaService.GetAllAsync();
            return Ok(plantillas.Select(p => new
            {
                id = p.Id,
                nombre = p.Nombre,
                created_at = p.CreatedAt
            }));
        }

        [HttpGet("{id}/archivo")]
        public async Task<IActionResult> GetArchivo(int id)
        {
            var resultado = await _plantillaService.GetArchivoAsync(id);
            if (resultado == null)
                return NotFound(new { error = "No existe una plantilla con ese id." });

            return File(resultado.Value.bytes, "application/pdf");
        }

        [HttpPost]
        public async Task<IActionResult> Subir([FromForm] string nombre, IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest(new { error = "El archivo es requerido." });

            if (!archivo.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "El archivo debe ser un PDF." });

            var plantilla = await _plantillaService.SubirAsync(nombre, archivo);

            return CreatedAtAction(nameof(GetAll), new
            {
                id = plantilla.Id,
                nombre = plantilla.Nombre,
                created_at = plantilla.CreatedAt
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var eliminado = await _plantillaService.EliminarAsync(id);
            if (!eliminado)
                return NotFound(new { error = "No existe una plantilla con ese id." });

            return Ok(new { mensaje = "Plantilla eliminada exitosamente." });
        }
    }
}