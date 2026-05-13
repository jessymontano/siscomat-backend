using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Siscomat.Api.Controllers;
using Siscomat.Core.Entities;
using Siscomat.Core.Interfaces;

namespace Siscomat.Tests.Controllers
{
    [TestFixture]
    public class PublicControllerTests
    {
        private Mock<IPublicService> _publicServiceMock;
        private PublicController _controller;

        [SetUp]
        public void SetUp()
        {
            _publicServiceMock = new Mock<IPublicService>();
            _controller = new PublicController(_publicServiceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        // ─── GetConstanciasByFolio ────────────────────────────────────────────────

        [Test]
        public async Task GetConstanciasByFolio_FolioExistenteConConstancias_ReturnsOkConLista()
        {
            // Arrange
            var curso = new Curso("Curso de Prueba") { Id = 1 };
            var participante = new Participante("2020-1234-1", "Ana", "López", "García");
            var constancia = new Constancia(participante, new Plantilla("p", "p.pdf"), curso) { Id = Guid.NewGuid() };
            participante.Constancias.Add(constancia);

            _publicServiceMock.Setup(s => s.GetConstanciasByFolioAsync("2020-1234-1"))
                .ReturnsAsync(participante);

            // Act
            var result = await _controller.GetConstanciasByFolio("2020-1234-1");

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Test]
        public async Task GetConstanciasByFolio_FolioExistenteSinConstancias_ReturnsOkConListaVacia()
        {
            // Arrange
            var participante = new Participante("2020-1234-1", "Ana", "López", "García");

            _publicServiceMock.Setup(s => s.GetConstanciasByFolioAsync("2020-1234-1"))
                .ReturnsAsync(participante);

            // Act
            var result = await _controller.GetConstanciasByFolio("2020-1234-1");

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            okResult.Value.Should().BeEquivalentTo(new
            {
                folio = "2020-1234-1",
                nombre = "Ana",
                apellido_1 = "López",
                apellido_2 = "García",
                constancias = Array.Empty<object>()
            });
        }

        [Test]
        public async Task GetConstanciasByFolio_FolioInexistente_ReturnsNotFound()
        {
            // Arrange
            _publicServiceMock.Setup(s => s.GetConstanciasByFolioAsync("0000-0000-0"))
                .ReturnsAsync((Participante?)null);

            // Act
            var result = await _controller.GetConstanciasByFolio("0000-0000-0");

            // Assert
            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.StatusCode.Should().Be(StatusCodes.Status404NotFound);
            notFound.Value.Should().BeEquivalentTo(new { error = "No existe un participante con ese folio." });
        }

        [Test]
        public async Task GetConstanciasByFolio_FolioVacio_ReturnsNotFound()
        {
            // Arrange
            _publicServiceMock.Setup(s => s.GetConstanciasByFolioAsync("   "))
                .ReturnsAsync((Participante?)null);

            // Act
            var result = await _controller.GetConstanciasByFolio("   ");

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>()
                .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }

        [Test]
        public async Task GetConstanciasByFolio_RespuestaConTodosLosCampos_RetornaEstructuraCompleta()
        {
            // Arrange
            var participante = new Participante("2020-1234-1", "Ana", "López", "García");

            _publicServiceMock.Setup(s => s.GetConstanciasByFolioAsync("2020-1234-1"))
                .ReturnsAsync(participante);

            // Act
            var result = await _controller.GetConstanciasByFolio("2020-1234-1");

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new
            {
                folio = "2020-1234-1",
                nombre = "Ana",
                apellido_1 = "López",
                apellido_2 = "García",
                constancias = Array.Empty<object>()
            });
        }

        [Test]
        public async Task GetConstanciasByFolio_ParticipanteConApellido2Nulo_RetornaApellido2Null()
        {
            // Arrange
            var participante = new Participante("2020-1234-1", "Ana", "López", null);

            _publicServiceMock.Setup(s => s.GetConstanciasByFolioAsync("2020-1234-1"))
                .ReturnsAsync(participante);

            // Act
            var result = await _controller.GetConstanciasByFolio("2020-1234-1");

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new
            {
                folio = "2020-1234-1",
                nombre = "Ana",
                apellido_1 = "López",
                apellido_2 = (string?)null,
                constancias = Array.Empty<object>()
            });
        }

        // ─── DownloadPdf ─────────────────────────────────────────────────────────

        [Test]
        public async Task DownloadPdf_IdValido_ReturnsFileResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            _publicServiceMock.Setup(s => s.GenerarPdfAsync(id))
                .ReturnsAsync(new byte[] { 1, 2, 3 });

            // Act
            var result = await _controller.DownloadPdf(id);

            // Assert
            result.Should().BeOfType<FileContentResult>();
        }

        [Test]
        public async Task DownloadPdf_IdValido_ContentTypeEsApplicationPdf()
        {
            // Arrange
            var id = Guid.NewGuid();
            _publicServiceMock.Setup(s => s.GenerarPdfAsync(id))
                .ReturnsAsync(new byte[] { 1, 2, 3 });

            // Act
            var result = await _controller.DownloadPdf(id);

            // Assert
            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            fileResult.ContentType.Should().Be("application/pdf");
        }

        [Test]
        public async Task DownloadPdf_IdValido_NombreArchivoEsCorrecto()
        {
            // Arrange
            var id = Guid.NewGuid();
            _publicServiceMock.Setup(s => s.GenerarPdfAsync(id))
                .ReturnsAsync(new byte[] { 1, 2, 3 });

            // Act
            var result = await _controller.DownloadPdf(id);

            // Assert
            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            fileResult.FileDownloadName.Should().Be($"constancia_{id}.pdf");
        }

        [Test]
        public async Task DownloadPdf_IdInexistente_ReturnsNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _publicServiceMock.Setup(s => s.GenerarPdfAsync(id))
                .ReturnsAsync((byte[]?)null);

            // Act
            var result = await _controller.DownloadPdf(id);

            // Assert
            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.StatusCode.Should().Be(StatusCodes.Status404NotFound);
            notFound.Value.Should().BeEquivalentTo(new { error = "No existe una constancia con ese id." });
        }

        // ─── ValidarConstancia ────────────────────────────────────────────────────

        [Test]
        public async Task ValidarConstancia_IdValido_ReturnsOk()
        {
            // Arrange
            var id = Guid.NewGuid();
            var participante = new Participante("2020-1234-1", "Ana", "López", "García");
            var curso = new Curso("Curso de Prueba");
            var plantilla = new Plantilla("Plantilla", "path.pdf");
            var constancia = new Constancia(participante, plantilla, curso) { Id = id };

            _publicServiceMock.Setup(s => s.ValidarConstanciaAsync(id))
                .ReturnsAsync(constancia);

            // Act
            var result = await _controller.ValidarConstancia(id);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Test]
        public async Task ValidarConstancia_IdValido_RespuestaConTodosLosCampos()
        {
            // Arrange
            var id = Guid.NewGuid();
            var participante = new Participante("2020-1234-1", "Ana", "López", "García");
            var curso = new Curso("Curso de Prueba");
            var plantilla = new Plantilla("Plantilla", "path.pdf");
            var constancia = new Constancia(participante, plantilla, curso) { Id = id };

            _publicServiceMock.Setup(s => s.ValidarConstanciaAsync(id))
                .ReturnsAsync(constancia);

            // Act
            var result = await _controller.ValidarConstancia(id);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new
            {
                id = id,
                participante = new
                {
                    folio = "2020-1234-1",
                    nombre = "Ana",
                    apellido_1 = "López",
                    apellido_2 = "García"
                },
                curso = "Curso de Prueba",
                created_at = constancia.CreatedAt
            });
        }

        [Test]
        public async Task ValidarConstancia_IdInexistente_ReturnsNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _publicServiceMock.Setup(s => s.ValidarConstanciaAsync(id))
                .ReturnsAsync((Constancia?)null);

            // Act
            var result = await _controller.ValidarConstancia(id);

            // Assert
            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.StatusCode.Should().Be(StatusCodes.Status404NotFound);
            notFound.Value.Should().BeEquivalentTo(new { error = "No existe una constancia con ese id." });
        }
    }
}