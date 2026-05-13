using Microsoft.AspNetCore.Mvc;
using Siscomat.Core.Interfaces;
using Siscomat.Services;

namespace Siscomat.Api.Controllers
{
    /// <summary>
    /// Controlador para manejar las operaciones relacionadas con las constancias. Proporciona endpoints para previsualizar constancias generadas a partir de plantillas y para cargar datos desde archivos CSV para generar constancias en masa. Utiliza el servicio ConstanciaService para realizar la lógica de negocio y manejar la comunicación con el motor de generación de PDF.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ConstanciasController(IConstanciaService constanciaService) : ControllerBase
    {
        private readonly IConstanciaService _constanciaService = constanciaService;

        /// <summary>
        /// Permite previsualizar una constancia generada a partir de una plantilla y datos específicos.
        /// </summary>
        /// <param name="request">Objeto que contiene la información necesaria para generar la constancia.</param>
        /// <returns>Objeto que contiene el estado de la operación, un mensaje descriptivo y el archivo de la constancia en formato Base64 si la generación fue exitosa.</returns>
        /// <response code="200">Previsualización generada exitosamente.</response>
        /// <response code="404">No se pudo generar la previsualización debido a que no se encontró la plantilla o algún recurso necesario.</response>
        /// <response code="400">Ocurrió un error al comunicarse con el motor de PDF o al procesar la solicitud.</response>
        [HttpPost("previsualizar")]
        [ProducesResponseType(typeof(PythonPreviewResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Permite cargar un archivo CSV para generar constancias en masa a partir de una plantilla específica. El endpoint procesa el archivo, valida su formato y genera las constancias correspondientes. Si se establece el parámetro "solo_validar" en true, el endpoint solo validará el archivo sin generar las constancias, devolviendo un resumen de los registros válidos e inválidos.
        /// </summary>
        /// <param name="plantilla_id">ID de la plantilla a utilizar para generar las constancias.</param>
        /// <param name="archivo">Archivo CSV que contiene los datos para generar las constancias.</param>
        /// <param name="solo_validar">Indica si solo se debe validar el archivo sin generar las constancias.</param>
        /// <returns>Objeto que contiene el estado de la operación, un mensaje descriptivo y un resumen de los registros válidos e inválidos si se realizó la validación.</returns>
        /// <response code="200">Archivo procesado exitosamente, con un resumen de los resultados.</response>
        /// <response code="400">Ocurrió un error al procesar el archivo, como formato incorrecto o falta de columnas requeridas.</response>
        /// <response code="404">Plantilla no encontrada.</response>
        /// <response code="500">Ocurrió un error interno al procesar el archivo.</response>
        [HttpPost("cargar")]
        [ProducesResponseType(typeof(CargaConstanciasResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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