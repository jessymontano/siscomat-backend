using Microsoft.AspNetCore.Mvc;
using Siscomat.Services;

namespace Siscomat.Api.Controllers
{
    [ApiController]
    [Route("api/public")]
    public class PublicController : ControllerBase
    {
        private readonly PublicService _publicService;

        public PublicController(PublicService publicService)
        {
            _publicService = publicService;
        }

        [HttpGet("constancia/{folio}")]
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

        [HttpGet("constancia/{id:Guid}/pdf")]
        public async Task<IActionResult> DownloadPdf(Guid id)
        {
            // TODO: implementar cuando el microservicio Python esté listo
            return StatusCode(501, new { error = "No implementado aún." });
        }

        [HttpGet("validar/{id:Guid}")]
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