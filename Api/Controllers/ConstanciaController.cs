using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Siscomat.Services;

namespace Siscomat.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConstanciasController : ControllerBase
    {
        private readonly ConstanciaService _constanciaService;

        public ConstanciasController(ConstanciaService constanciaService)
        {
            _constanciaService = constanciaService;
        }

        [HttpPost("previsualizar")]
        public async Task<IActionResult> PrevisualizarConstancia([FromBody] PrevisualizarRequest request)
        {
            try
            {
                var response = await _constanciaService.PrevisualizarAsync(request);
                return Ok(response);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { error = "No se pudo generar la previsualización.", detalle = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Ocurrió un error al comunicarse con el motor de PDF.", detalle = ex.Message });
            }
        }

        [HttpPost("cargar")]
        public async Task<IActionResult> CargarConstancias([FromForm] int plantilla_id, IFormFile archivo, [FromForm] bool solo_validar = false)
        {
            if (archivo == null || archivo.Length == 0)
            {
                return BadRequest(new { error = "Por favor, seleccione un archivo CSV válido." });
            }

            try
            {
                var response = await _constanciaService.ProcesarCargaCsvAsync(plantilla_id, archivo, solo_validar);
                return Ok(response);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { error = "Plantilla no encontrada.", detalle = ex.Message });
            }
            catch (CsvHelper.MissingFieldException ex)
            {
                return BadRequest(new { error = "El archivo CSV no tiene el formato correcto. Faltan columnas requeridas.", detalle = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Ocurrió un error interno al procesar el archivo.", detalle = ex.Message });
            }
        }
    }
}