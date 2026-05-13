using FluentAssertions;
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
    public class PublicServiceTests
    {
        private Mock<IParticipanteRepository> _participanteRepoMock;
        private Mock<IConstanciaRepository> _constanciaRepoMock;
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private Mock<HttpMessageHandler> _httpHandlerMock;
        private IConfiguration _configuration;
        private PublicService _service;

        [SetUp]
        public void SetUp()
        {
            _participanteRepoMock = new Mock<IParticipanteRepository>();
            _constanciaRepoMock = new Mock<IConstanciaRepository>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpHandlerMock = new Mock<HttpMessageHandler>();

            var configValues = new Dictionary<string, string?>
            {
                { "FrontendSettings:PortalPublicoUrl", "http://localhost:3000" },
                { "MicroservicioSettings:ApiKey", "test-api-key" },
                { "MicroservicioSettings:Url", "http://localhost:8000" }
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            var httpClient = new HttpClient(_httpHandlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:8000")
            };
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _service = new PublicService(
                _participanteRepoMock.Object,
                _constanciaRepoMock.Object,
                _httpClientFactoryMock.Object,
                _configuration
            );
        }

        // ─── GetConstanciasByFolioAsync ──────────────────────────────────────────

        [Test]
        public async Task GetConstanciasByFolioAsync_FolioExistente_ReturnsParticipante()
        {
            // Arrange
            var participante = new Participante("2020-1234-1", "Ana", "López", "García");
            _participanteRepoMock.Setup(r => r.GetByFolioWithConstanciasAsync("2020-1234-1"))
                .ReturnsAsync(participante);

            // Act
            var resultado = await _service.GetConstanciasByFolioAsync("2020-1234-1");

            // Assert
            resultado.Should().NotBeNull();
            resultado!.Folio.Should().Be("2020-1234-1");
        }

        [Test]
        public async Task GetConstanciasByFolioAsync_FolioInexistente_ReturnsNull()
        {
            // Arrange
            _participanteRepoMock.Setup(r => r.GetByFolioWithConstanciasAsync("0000-0000-0"))
                .ReturnsAsync((Participante?)null);

            // Act
            var resultado = await _service.GetConstanciasByFolioAsync("0000-0000-0");

            // Assert
            resultado.Should().BeNull();
        }

        // ─── ValidarConstanciaAsync ──────────────────────────────────────────────

        [Test]
        public async Task ValidarConstanciaAsync_IdExistente_ReturnsConstancia()
        {
            // Arrange
            var id = Guid.NewGuid();
            var constancia = new Constancia();
            _constanciaRepoMock.Setup(r => r.GetByIdWithDetailsAsync(id)).ReturnsAsync(constancia);

            // Act
            var resultado = await _service.ValidarConstanciaAsync(id);

            // Assert
            resultado.Should().NotBeNull();
        }

        [Test]
        public async Task ValidarConstanciaAsync_IdInexistente_ReturnsNull()
        {
            // Arrange
            var id = Guid.NewGuid();
            _constanciaRepoMock.Setup(r => r.GetByIdWithDetailsAsync(id)).ReturnsAsync((Constancia?)null);

            // Act
            var resultado = await _service.ValidarConstanciaAsync(id);

            // Assert
            resultado.Should().BeNull();
        }

        // ─── GenerarPdfAsync ─────────────────────────────────────────────────────

        [Test]
        public async Task GenerarPdfAsync_ConstanciaInexistente_ReturnsNullSinLlamarMicroservicio()
        {
            // Arrange
            var id = Guid.NewGuid();
            _constanciaRepoMock.Setup(r => r.GetByIdWithDetailsAsync(id)).ReturnsAsync((Constancia?)null);

            // Act
            var resultado = await _service.GenerarPdfAsync(id);

            // Assert
            resultado.Should().BeNull();
            _httpHandlerMock.Protected().Verify("SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Test]
        public async Task GenerarPdfAsync_MicroservicioDevuelveError_ReturnsNull()
        {
            // Arrange
            var id = Guid.NewGuid();
            var tempFile = Path.GetTempFileName();
            await File.WriteAllBytesAsync(tempFile, new byte[] { 1, 2, 3 });

            var participante = new Participante("2020-1234-1", "Ana", "López", "García");
            var curso = new Curso("Curso de Prueba");
            var plantilla = new Plantilla("Plantilla", tempFile);
            var constancia = new Constancia(participante, plantilla, curso);

            _constanciaRepoMock.Setup(r => r.GetByIdWithDetailsAsync(id)).ReturnsAsync(constancia);

            _httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            // Act
            var resultado = await _service.GenerarPdfAsync(id);

            // Assert
            resultado.Should().BeNull();

            // Cleanup
            File.Delete(tempFile);
        }

        [Test]
        public async Task GenerarPdfAsync_ParticipanteSinApellido2_NombreSinEspaciosDobles()
        {
            // Arrange
            var id = Guid.NewGuid();
            var tempFile = Path.GetTempFileName();
            await File.WriteAllBytesAsync(tempFile, new byte[] { 1, 2, 3 });

            var participante = new Participante("2020-1234-1", "Ana", "López", ""); // sin apellido2
            var curso = new Curso("Curso de Prueba");
            var plantilla = new Plantilla("Plantilla", tempFile);
            var constancia = new Constancia(participante, plantilla, curso);

            _constanciaRepoMock.Setup(r => r.GetByIdWithDetailsAsync(id)).ReturnsAsync(constancia);

            string? nombreCapturado = null;
            _httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
                {
                    var body = await req.Content!.ReadAsStringAsync();
                    var doc = JsonDocument.Parse(body);
                    nombreCapturado = doc.RootElement.GetProperty("nombre_participante").GetString();
                })
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            // Act
            await _service.GenerarPdfAsync(id);

            // Assert
            nombreCapturado.Should().NotContain("  "); // sin espacios dobles
            nombreCapturado.Should().Be("Ana López");

            // Cleanup
            File.Delete(tempFile);
        }
    }
}