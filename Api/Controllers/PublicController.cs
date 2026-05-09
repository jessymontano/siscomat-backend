using Microsoft.AspNetCore.Mvc;
using Siscomat.Services;

namespace Siscomat.Api.Controllers
{
    /// <summary>
    /// Controlador para manejar los endpoints públicos relacionados con las constancias. Proporciona endpoints para que los participantes puedan consultar sus constancias utilizando su folio, descargar el PDF de una constancia específica y validar la autenticidad de una constancia utilizando su ID. Este controlador no requiere autenticación, ya que está diseñado para ser accesible por cualquier persona que tenga la información necesaria para realizar las consultas. Utiliza el servicio PublicService para realizar la lógica de negocio y manejar la comunicación con la base de datos y el motor de generación de PDF.
    /// </summary>
    [ApiController]
    [Route("api/public")]
    public class PublicController : ControllerBase
    {
        private readonly PublicService _publicService;

        public PublicController(PublicService publicService)
        {
            _publicService = publicService;
        }

        /// <summary>
        /// Obtiene las constancias asociadas a un participante utilizando su folio. El endpoint recibe el folio como parámetro en la URL y devuelve la información del participante junto con una lista de sus constancias, incluyendo el ID de cada constancia y el nombre del curso asociado. Si no se encuentra un participante con el folio proporcionado, se devuelve un error 404 con un mensaje indicando que no existe un participante con ese folio.
        /// </summary>
        /// <param name="folio">Folio del participante.</param>
        /// <returns>Información del participante y sus constancias.</returns>
        /// <response code="200">Constancias obtenidas exitosamente.</response>
        /// <response code="404">No se encontró un participante con el folio proporcionado.</response>
        [HttpGet("constancia/{folio}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetConstanciasByFolio(string folio)
        {
            var participante = await _publicService.GetConstanciasByFolioAsync(folio);

            if (participante == null)
                return NotFound(new { error = "No existe un participante con ese folio." });

            return Ok(new
            {
                folio = participante.Folio,
                nombre = participante.Nombre,
                apellido_1 = participante.Apellido1,
                apellido_2 = participante.Apellido2,
                constancias = participante.Constancias.Select(c => new
                {
                    id = c.Id,
                    curso = c.Curso.Nombre
                })
            });
        }

        /// <summary>
        /// Permite descargar el PDF de una constancia específica utilizando su ID. El endpoint recibe el ID de la constancia como parámetro en la URL, genera el PDF correspondiente utilizando el servicio PublicService y devuelve el archivo PDF como respuesta. Si no se encuentra una constancia con el ID proporcionado, se devuelve un error 404 con un mensaje indicando que no existe una constancia con ese ID.
        /// </summary>
        /// <param name="id">ID de la constancia a descargar.</param>
        /// <returns>Archivo PDF de la constancia.</returns>
        /// <response code="200">Archivo PDF de la constancia obtenido exitosamente.</response>
        /// <response code="404">No se encontró una constancia con el ID proporcionado.</response>
        [HttpGet("constancia/{id:Guid}/pdf")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadPdf(Guid id)
        {
            var pdfBytes = await _publicService.GenerarPdfAsync(id);
            
            if (pdfBytes == null) 
                return NotFound(new { error = "No existe una constancia con ese id." });

            return File(pdfBytes, "application/pdf", $"constancia_{id}.pdf");
        }

        /// <summary>
        /// Valida la autenticidad de una constancia utilizando su ID. El endpoint recibe el ID de la constancia como parámetro en la URL, verifica si existe una constancia con ese ID utilizando el servicio PublicService y devuelve la información de la constancia si es válida. Si no se encuentra una constancia con el ID proporcionado, se devuelve un error 404 con un mensaje indicando que no existe una constancia con ese ID. Este endpoint es útil para que terceros puedan verificar la autenticidad de una constancia sin necesidad de acceder a información sensible del participante o del curso.
        /// </summary>
        /// <param name="id">ID de la constancia a validar.</param>
        /// <returns>Información de la constancia si es válida.</returns>
        /// <response code="200">Constancia válida.</response>
        /// <response code="404">No se encontró una constancia con el ID proporcionado.</response>
        [HttpGet("validar/{id:Guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ValidarConstancia(Guid id)
        {
            var constancia = await _publicService.ValidarConstanciaAsync(id);

            if (constancia == null)
                return NotFound(new { error = "No existe una constancia con ese id." });

            return Ok(new
            {
                id = constancia.Id,
                participante = new
                {
                    folio = constancia.Participante.Folio,
                    nombre = constancia.Participante.Nombre,
                    apellido_1 = constancia.Participante.Apellido1,
                    apellido_2 = constancia.Participante.Apellido2
                },
                curso = constancia.Curso.Nombre,
                created_at = constancia.CreatedAt
            });
        }
    }
}