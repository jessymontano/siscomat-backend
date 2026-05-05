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
                created_at = p.CreatedAt,
                en_uso = p.Constancias.Any()
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

        [HttpDelete("{id}")]
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