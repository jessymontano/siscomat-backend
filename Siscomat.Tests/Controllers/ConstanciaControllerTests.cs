using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Siscomat.Api.Controllers;
using Siscomat.Core.Interfaces;
using Siscomat.Services;
using System.Text;

namespace Siscomat.Tests.Controllers
{
    [TestFixture]
    public class ConstanciasControllerTests
    {
        private Mock<IConstanciaService> _constanciaServiceMock;
        private ConstanciasController _controller;

        [SetUp]
        public void SetUp()
        {
            _constanciaServiceMock = new Mock<IConstanciaService>();
            _controller = new ConstanciasController(_constanciaServiceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        private IFormFile CrearArchivoCsv(string contenido = "folio,nombre,apellido1,apellido2,curso\n2020-1234-1,Ana,López,,Curso A")
        {
            var bytes = Encoding.UTF8.GetBytes(contenido);
            var stream = new MemoryStream(bytes);
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
            fileMock.Setup(f => f.Length).Returns(bytes.Length);
            fileMock.Setup(f => f.FileName).Returns("datos.csv");
            fileMock.Setup(f => f.ContentType).Returns("text/csv");
            return fileMock.Object;
        }

        // ─── PrevisualizarConstancia ──────────────────────────────────────────────

        [Test]
        public async Task PrevisualizarConstancia_DatosValidosYPlantillaExistente_ReturnsOk()
        {
            // Arrange
            var request = new PrevisualizarRequest
            {
                folio = "2020-1234-1",
                nombre_participante = "Ana López",
                nombre_curso = "Curso de Prueba",
                plantilla_id = 1
            };

            var response = new PythonPreviewResponse
            {
                estado = "ok",
                mensaje = "Previsualización generada",
                archivo_base64 = "base64string"
            };

            _constanciaServiceMock.Setup(s => s.PrevisualizarAsync(request))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.PrevisualizarConstancia(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            okResult.Value.Should().BeEquivalentTo(response);
        }

        [Test]
        public async Task PrevisualizarConstancia_PlantillaInexistente_ReturnsNotFound()
        {
            // Arrange
            var request = new PrevisualizarRequest { plantilla_id = 99 };

            _constanciaServiceMock.Setup(s => s.PrevisualizarAsync(request))
                .ThrowsAsync(new FileNotFoundException("La plantilla seleccionada no existe en el servidor."));

            // Act
            var result = await _controller.PrevisualizarConstancia(request);

            // Assert
            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.StatusCode.Should().Be(StatusCodes.Status404NotFound);
            notFound.Value.Should().BeEquivalentTo(new
            {
                error = "No se pudo generar la previsualización.",
                detalle = "La plantilla seleccionada no existe en el servidor."
            });
        }

        [Test]
        public async Task PrevisualizarConstancia_MotorPdfFalla_ReturnsBadRequest()
        {
            // Arrange
            var request = new PrevisualizarRequest { plantilla_id = 1 };

            _constanciaServiceMock.Setup(s => s.PrevisualizarAsync(request))
                .ThrowsAsync(new Exception("Error en el generador: timeout"));

            // Act
            var result = await _controller.PrevisualizarConstancia(request);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            badRequest.Value.Should().BeEquivalentTo(new
            {
                error = "Ocurrió un error al comunicarse con el motor de PDF.",
                detalle = "Error en el generador: timeout"
            });
        }

        // ─── CargarConstancias ────────────────────────────────────────────────────

        [Test]
        public async Task CargarConstancias_CsvValidoConPlantillaExistente_ReturnsOk()
        {
            // Arrange
            var archivo = CrearArchivoCsv();
            var response = new CargaConstanciasResponse(1, 1, new List<ErrorFila>());

            _constanciaServiceMock.Setup(s => s.ProcesarCargaCsvAsync(1, archivo, false))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.CargarConstancias(1, archivo);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            okResult.Value.Should().BeEquivalentTo(response);
        }

        [Test]
        public async Task CargarConstancias_CsvConFilasValidasEInvalidas_ReturnsOkConErrores()
        {
            // Arrange
            var archivo = CrearArchivoCsv();
            var errores = new List<ErrorFila> { new ErrorFila(3, "Folio inválido") };
            var response = new CargaConstanciasResponse(1, 1, errores);

            _constanciaServiceMock.Setup(s => s.ProcesarCargaCsvAsync(1, archivo, false))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.CargarConstancias(1, archivo);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Test]
        public async Task CargarConstancias_SinArchivo_ReturnsBadRequestSinConsultarBD()
        {
            // Act
            var result = await _controller.CargarConstancias(1, null!);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            badRequest.Value.Should().BeEquivalentTo(new { error = "Por favor, seleccione un archivo CSV válido." });

            _constanciaServiceMock.Verify(s => s.ProcesarCargaCsvAsync(
                It.IsAny<int>(), It.IsAny<IFormFile>(), It.IsAny<bool>()), Times.Never);
        }

        [Test]
        public async Task CargarConstancias_PlantillaInexistente_ReturnsNotFound()
        {
            // Arrange
            var archivo = CrearArchivoCsv();

            _constanciaServiceMock.Setup(s => s.ProcesarCargaCsvAsync(99, archivo, false))
                .ThrowsAsync(new FileNotFoundException("La plantilla no existe."));

            // Act
            var result = await _controller.CargarConstancias(99, archivo);

            // Assert
            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.StatusCode.Should().Be(StatusCodes.Status404NotFound);
            notFound.Value.Should().BeEquivalentTo(new
            {
                error = "Plantilla no encontrada.",
                detalle = "La plantilla no existe."
            });
        }

        [Test]
        public async Task CargarConstancias_CsvConColumnasFaltantes_ReturnsBadRequest()
        {
            // Arrange
            var archivo = CrearArchivoCsv("folio,nombre\n2020-1234-1,Ana"); // sin columnas requeridas

            var missingFieldException = (CsvHelper.MissingFieldException)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(CsvHelper.MissingFieldException));

            _constanciaServiceMock.Setup(s => s.ProcesarCargaCsvAsync(1, archivo, false))
                .ThrowsAsync(missingFieldException);

            // Act
            var result = await _controller.CargarConstancias(1, archivo);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Test]
        public async Task CargarConstancias_CsvVacio_ReturnsOkConCeroRegistros()
        {
            // Arrange
            var archivo = CrearArchivoCsv("folio,nombre,apellido1,apellido2,curso");
            var response = new CargaConstanciasResponse(0, 0, new List<ErrorFila>());

            _constanciaServiceMock.Setup(s => s.ProcesarCargaCsvAsync(1, archivo, false))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.CargarConstancias(1, archivo);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(response);
        }

        [Test]
        public async Task CargarConstancias_SoloValidarTrue_ReturnsOkSinGuardar()
        {
            // Arrange
            var archivo = CrearArchivoCsv();
            var response = new CargaConstanciasResponse(0, 1, new List<ErrorFila>());

            _constanciaServiceMock.Setup(s => s.ProcesarCargaCsvAsync(1, archivo, true))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.CargarConstancias(1, archivo, solo_validar: true);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Test]
        public async Task CargarConstancias_SoloValidarTrueConErrores_ReturnsOkConResumen()
        {
            // Arrange
            var archivo = CrearArchivoCsv();
            var errores = new List<ErrorFila> { new ErrorFila(2, "Folio inválido") };
            var response = new CargaConstanciasResponse(0, 0, errores);

            _constanciaServiceMock.Setup(s => s.ProcesarCargaCsvAsync(1, archivo, true))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.CargarConstancias(1, archivo, solo_validar: true);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            okResult.Value.Should().BeEquivalentTo(response);
        }

        [Test]
        public async Task CargarConstancias_ErrorInterno_Returns500()
        {
            // Arrange
            var archivo = CrearArchivoCsv();

            _constanciaServiceMock.Setup(s => s.ProcesarCargaCsvAsync(1, archivo, false))
                .ThrowsAsync(new Exception("Error inesperado en el servidor."));

            // Act
            var result = await _controller.CargarConstancias(1, archivo);

            // Assert
            var serverError = result.Should().BeAssignableTo<ObjectResult>().Subject;
            serverError.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        }
    }
}