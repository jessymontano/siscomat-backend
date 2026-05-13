using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Siscomat.Api.Controllers;
using Siscomat.Core.Entities;
using Siscomat.Core.Interfaces;
using Siscomat.Services;
using System.Security.Claims;

namespace Siscomat.Tests.Controllers
{
    [TestFixture]
    public class PlantillaControllerTests
    {
        private Mock<IPlantillaService> _plantillaServiceMock;
        private PlantillaController _controller;

        [SetUp]
        public void SetUp()
        {
            _plantillaServiceMock = new Mock<IPlantillaService>();
            _controller = new PlantillaController(_plantillaServiceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        private void SetupUsuarioAutenticado()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "Gestor de Prueba"),
                new Claim(ClaimTypes.Email, "gestor@unison.mx")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
        }

        // ─── GetAll ───────────────────────────────────────────────────────────────

        [Test]
        public async Task GetAll_UsuarioAutenticado_ReturnsOkConLista()
        {
            // Arrange
            SetupUsuarioAutenticado();
            var plantillas = new List<Plantilla>
            {
                new Plantilla("Plantilla A", "pathA.pdf"),
                new Plantilla("Plantilla B", "pathB.pdf")
            };

            _plantillaServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(plantillas);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Test]
        public async Task GetAll_SinPlantillas_ReturnsOkConListaVacia()
        {
            // Arrange
            SetupUsuarioAutenticado();
            _plantillaServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<Plantilla>());

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            okResult.Value.Should().BeEquivalentTo(Array.Empty<object>());
        }

        [Test]
        public async Task GetAll_PlantillaConConstancias_CampoEnUsoEsTrue()
        {
            // Arrange
            SetupUsuarioAutenticado();
            var plantilla = new Plantilla("Plantilla A", "pathA.pdf");
            plantilla.Constancias.Add(new Constancia());

            _plantillaServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<Plantilla> { plantilla });

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var lista = okResult.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject.ToList();
            lista[0].Should().BeEquivalentTo(new
            {
                id = plantilla.Id,
                nombre = "Plantilla A",
                created_at = plantilla.CreatedAt,
                en_uso = true
            });
        }

        [Test]
        public async Task GetAll_PlantillaSinConstancias_CampoEnUsoEsFalse()
        {
            // Arrange
            SetupUsuarioAutenticado();
            var plantilla = new Plantilla("Plantilla A", "pathA.pdf");

            _plantillaServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<Plantilla> { plantilla });

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var lista = okResult.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject.ToList();
            lista[0].Should().BeEquivalentTo(new
            {
                id = plantilla.Id,
                nombre = "Plantilla A",
                created_at = plantilla.CreatedAt,
                en_uso = false
            });
        }

        // ─── GetArchivo ───────────────────────────────────────────────────────────

        [Test]
        public async Task GetArchivo_PlantillaExistente_ReturnsFilePdf()
        {
            // Arrange
            SetupUsuarioAutenticado();
            _plantillaServiceMock.Setup(s => s.GetArchivoAsync(1))
                .ReturnsAsync((new byte[] { 1, 2, 3 }, "pathA.pdf"));

            // Act
            var result = await _controller.GetArchivo(1);

            // Assert
            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            fileResult.ContentType.Should().Be("application/pdf");
        }

        [Test]
        public async Task GetArchivo_PlantillaInexistente_ReturnsNotFound()
        {
            // Arrange
            SetupUsuarioAutenticado();
            _plantillaServiceMock.Setup(s => s.GetArchivoAsync(99))
                .ReturnsAsync(((byte[], string)?)null);

            // Act
            var result = await _controller.GetArchivo(99);

            // Assert
            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.StatusCode.Should().Be(StatusCodes.Status404NotFound);
            notFound.Value.Should().BeEquivalentTo(new { error = "No existe una plantilla con ese id." });
        }

        // ─── Subir ────────────────────────────────────────────────────────────────

        [Test]
        public async Task Subir_PlantillaValidaConPlaceholders_ReturnsCreated()
        {
            // Arrange
            SetupUsuarioAutenticado();
            var plantilla = new Plantilla("Mi Plantilla", "path.pdf");

            var archivoMock = new Mock<IFormFile>();
            archivoMock.Setup(f => f.Length).Returns(100);
            archivoMock.Setup(f => f.ContentType).Returns("application/pdf");

            _plantillaServiceMock.Setup(s => s.SubirAsync("Mi Plantilla", archivoMock.Object))
                .ReturnsAsync(plantilla);

            // Act
            var result = await _controller.Subir("Mi Plantilla", archivoMock.Object);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        }

        [Test]
        public async Task Subir_SinArchivo_ReturnsBadRequest()
        {
            // Arrange
            SetupUsuarioAutenticado();

            // Act
            var result = await _controller.Subir("Mi Plantilla", null!);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            badRequest.Value.Should().BeEquivalentTo(new { error = "El archivo es requerido." });
        }

        [Test]
        public async Task Subir_ArchivoNoEsPdf_ReturnsBadRequest()
        {
            // Arrange
            SetupUsuarioAutenticado();
            var archivoMock = new Mock<IFormFile>();
            archivoMock.Setup(f => f.Length).Returns(100);
            archivoMock.Setup(f => f.ContentType).Returns("image/png");

            // Act
            var result = await _controller.Subir("Mi Plantilla", archivoMock.Object);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            badRequest.Value.Should().BeEquivalentTo(new { error = "El archivo debe ser un PDF." });
        }

        [Test]
        public async Task Subir_PlantillaSinPlaceholders_ReturnsBadRequestConDetalles()
        {
            // Arrange
            SetupUsuarioAutenticado();
            var archivoMock = new Mock<IFormFile>();
            archivoMock.Setup(f => f.Length).Returns(100);
            archivoMock.Setup(f => f.ContentType).Returns("application/pdf");

            var detalles = new ValidacionPlantillaResponse(
                es_valida: false,
                placeholders_encontrados: new List<string>(),
                placeholders_faltantes: new List<string> { "{{nombre}}", "{{curso}}" }
            );

            _plantillaServiceMock.Setup(s => s.SubirAsync(It.IsAny<string>(), It.IsAny<IFormFile>()))
                .ThrowsAsync(new PlantillaInvalidaException(detalles));

            // Act
            var result = await _controller.Subir("Mi Plantilla", archivoMock.Object);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            badRequest.Value.Should().BeEquivalentTo(new
            {
                error = "La plantilla no cuenta con los placeholders requeridos.",
                detalles = detalles
            });
        }

        // ─── Eliminar ─────────────────────────────────────────────────────────────

        [Test]
        public async Task Eliminar_PlantillaSinConstancias_ReturnsOk()
        {
            // Arrange
            SetupUsuarioAutenticado();
            _plantillaServiceMock.Setup(s => s.EliminarAsync(1))
                .ReturnsAsync((true, false));

            // Act
            var result = await _controller.Eliminar(1);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            okResult.Value.Should().BeEquivalentTo(new { mensaje = "Plantilla eliminada exitosamente." });
        }

        [Test]
        public async Task Eliminar_PlantillaConConstancias_ReturnsConflict()
        {
            // Arrange
            SetupUsuarioAutenticado();
            _plantillaServiceMock.Setup(s => s.EliminarAsync(1))
                .ReturnsAsync((true, true));

            // Act
            var result = await _controller.Eliminar(1);

            // Assert
            var conflict = result.Should().BeOfType<ConflictObjectResult>().Subject;
            conflict.StatusCode.Should().Be(StatusCodes.Status409Conflict);
            conflict.Value.Should().BeEquivalentTo(new { error = "La plantilla ya fue usada para generar constancias y no puede eliminarse." });
        }

        [Test]
        public async Task Eliminar_PlantillaInexistente_ReturnsNotFound()
        {
            // Arrange
            SetupUsuarioAutenticado();
            _plantillaServiceMock.Setup(s => s.EliminarAsync(99))
                .ReturnsAsync((false, false));

            // Act
            var result = await _controller.Eliminar(99);

            // Assert
            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.StatusCode.Should().Be(StatusCodes.Status404NotFound);
            notFound.Value.Should().BeEquivalentTo(new { error = "No existe una plantilla con ese id." });
        }
    }
}