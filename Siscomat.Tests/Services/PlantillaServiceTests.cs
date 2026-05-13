using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Siscomat.Core.Entities;
using Siscomat.Core.Interfaces;
using Siscomat.Services;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Siscomat.Tests.Services
{
    [TestFixture]
    public class PlantillaServiceTests
    {
        private Mock<IPlantillaRepository> _plantillaRepoMock;
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private Mock<HttpMessageHandler> _httpHandlerMock;
        private IConfiguration _configuration;
        private PlantillaService _service;

        [SetUp]
        public void SetUp()
        {
            _plantillaRepoMock = new Mock<IPlantillaRepository>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpHandlerMock = new Mock<HttpMessageHandler>();

            var configValues = new Dictionary<string, string?>
            {
                { "Storage:PlantillasPath", "plantillas_test" },
                { "MicroservicioSettings:Url", "http://localhost:8000" },
                { "MicroservicioSettings:ApiKey", "test-api-key" }
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            var httpClient = new HttpClient(_httpHandlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _service = new PlantillaService(
                _plantillaRepoMock.Object,
                _configuration,
                _httpClientFactoryMock.Object
            );
        }

        // ─── EliminarAsync ───────────────────────────────────────────────────────

        [Test]
        public async Task EliminarAsync_PlantillaInexistente_ReturnsFalseFalse()
        {
            // Arrange
            _plantillaRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Plantilla?)null);

            // Act
            var (eliminado, enUso) = await _service.EliminarAsync(99);

            // Assert
            eliminado.Should().BeFalse();
            enUso.Should().BeFalse();
        }

        [Test]
        public async Task EliminarAsync_PlantillaConConstancias_ReturnsTrueTrue()
        {
            // Arrange
            var plantilla = new Plantilla("Mi Plantilla", "path/falso.pdf");
            plantilla.Constancias.Add(new Constancia()); // simula que ya tiene constancias

            _plantillaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(plantilla);

            // Act
            var (eliminado, enUso) = await _service.EliminarAsync(1);

            // Assert
            eliminado.Should().BeTrue();
            enUso.Should().BeTrue();
            _plantillaRepoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task EliminarAsync_PlantillaSinConstancias_ReturnsTrueFalseYElimina()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var plantilla = new Plantilla("Mi Plantilla", tempFile);

            _plantillaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(plantilla);
            _plantillaRepoMock.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);
            _plantillaRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

            // Act
            var (eliminado, enUso) = await _service.EliminarAsync(1);

            // Assert
            eliminado.Should().BeTrue();
            enUso.Should().BeFalse();
            _plantillaRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
            File.Exists(tempFile).Should().BeFalse();
        }

        // ─── GetArchivoAsync ─────────────────────────────────────────────────────

        [Test]
        public async Task GetArchivoAsync_PlantillaInexistente_ReturnsNull()
        {
            // Arrange
            _plantillaRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Plantilla?)null);

            // Act
            var resultado = await _service.GetArchivoAsync(99);

            // Assert
            resultado.Should().BeNull();
        }

        [Test]
        public async Task GetArchivoAsync_ArchivoNoExisteEnDisco_ReturnsNull()
        {
            // Arrange
            var plantilla = new Plantilla("Mi Plantilla", "ruta/que/no/existe.pdf");
            _plantillaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(plantilla);

            // Act
            var resultado = await _service.GetArchivoAsync(1);

            // Assert
            resultado.Should().BeNull();
        }

        [Test]
        public async Task GetArchivoAsync_PlantillaExisteEnDisco_ReturnsBytesYPath()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            await File.WriteAllBytesAsync(tempFile, new byte[] { 1, 2, 3 });

            var plantilla = new Plantilla("Mi Plantilla", tempFile);
            _plantillaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(plantilla);

            // Act
            var resultado = await _service.GetArchivoAsync(1);

            // Assert
            resultado.Should().NotBeNull();
            resultado!.Value.bytes.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
            resultado.Value.path.Should().Be(tempFile);

            // Cleanup
            File.Delete(tempFile);
        }

        // ─── SubirAsync ──────────────────────────────────────────────────────────

        [Test]
        public async Task SubirAsync_MicroservicioDevuelveError_ThrowsException()
        {
            // Arrange
            _httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var archivoMock = new Mock<IFormFile>();
            archivoMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var act = async () => await _service.SubirAsync("Mi Plantilla", archivoMock.Object);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Error al comunicarse con el microservicio de validación.");
        }

        [Test]
        public async Task SubirAsync_PlantillaSinPlaceholders_ThrowsPlantillaInvalidaException()
        {
            // Arrange
            var validacionResponse = new ValidacionPlantillaResponse(
                es_valida: false,
                placeholders_encontrados: new List<string>(),
                placeholders_faltantes: new List<string> { "{{nombre}}", "{{curso}}" }
            );

            var json = JsonSerializer.Serialize(validacionResponse);

            _httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });

            var archivoMock = new Mock<IFormFile>();
            archivoMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var act = async () => await _service.SubirAsync("Mi Plantilla", archivoMock.Object);

            // Assert
            await act.Should().ThrowAsync<PlantillaInvalidaException>()
                .Where(ex => ex.Detalles.placeholders_faltantes.Contains("{{nombre}}"));
        }
    }
}