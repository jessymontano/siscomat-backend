using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Siscomat.Core.Entities;
using Siscomat.Core.Interfaces;
using Siscomat.Repositories;
using Siscomat.Services;
using System.Text;

namespace Siscomat.Tests.Services
{
    [TestFixture]
    public class ConstanciaServiceTests
    {
        private Mock<IConstanciaRepository> _constanciaRepoMock;
        private Mock<IParticipanteRepository> _participanteRepoMock;
        private Mock<IPlantillaRepository> _plantillaRepoMock;
        private Mock<ICursoRepository> _cursoRepoMock;
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private ApplicationDbContext _dbContext;
        private IConfiguration _configuration;
        private ConstanciaService _service;

        [SetUp]
        public void SetUp()
        {
            _constanciaRepoMock = new Mock<IConstanciaRepository>();
            _participanteRepoMock = new Mock<IParticipanteRepository>();
            _plantillaRepoMock = new Mock<IPlantillaRepository>();
            _cursoRepoMock = new Mock<ICursoRepository>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();

            // DbContext en memoria para que el servicio pueda leer los MaxLength del modelo
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);

            var configValues = new Dictionary<string, string?>
            {
                { "MicroservicioSettings:Url", "http://localhost:8000" },
                { "MicroservicioSettings:ApiKey", "test-api-key" }
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient());

            _service = new ConstanciaService(
                _constanciaRepoMock.Object,
                _participanteRepoMock.Object,
                _plantillaRepoMock.Object,
                _cursoRepoMock.Object,
                _httpClientFactoryMock.Object,
                _configuration,
                _dbContext
            );
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Dispose();
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private void SetupReposVacios()
        {
            _cursoRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Curso>());
            _participanteRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Participante>());
            _constanciaRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Constancia>());
            _participanteRepoMock.Setup(r => r.AddAsync(It.IsAny<Participante>())).Returns(Task.CompletedTask);
            _cursoRepoMock.Setup(r => r.AddAsync(It.IsAny<Curso>())).Returns(Task.CompletedTask);
            _constanciaRepoMock.Setup(r => r.AddAsync(It.IsAny<Constancia>())).Returns(Task.CompletedTask);
            _constanciaRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);
        }

        private IFormFile CrearCsv(string contenido)
        {
            var bytes = Encoding.UTF8.GetBytes(contenido);
            var stream = new MemoryStream(bytes);
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
            fileMock.Setup(f => f.Length).Returns(bytes.Length);
            return fileMock.Object;
        }

        private Plantilla CrearPlantillaFake()
        {
            var tempFile = Path.GetTempFileName();
            return new Plantilla("Plantilla Test", tempFile);
        }

        // ─── ProcesarCargaCsvAsync ────────────────────────────────────────────────

        [Test]
        public async Task ProcesarCargaCsvAsync_PlantillaInexistente_ThrowsFileNotFoundException()
        {
            // Arrange
            _plantillaRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Plantilla?)null);
            var archivo = CrearCsv("folio,nombre,apellido1,apellido2,curso\n2020-1234-1,Ana,López,,Curso");

            // Act
            var act = async () => await _service.ProcesarCargaCsvAsync(99, archivo);

            // Assert
            await act.Should().ThrowAsync<FileNotFoundException>();
        }

        [Test]
        public async Task ProcesarCargaCsvAsync_CsvValido_RegistraParticipantesYConstancias()
        {
            // Arrange
            var plantilla = CrearPlantillaFake();
            _plantillaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(plantilla);
            SetupReposVacios();

            var csv = "folio,nombre,apellido1,apellido2,curso\n2020-1234-1,Ana,López,García,Curso de Prueba";
            var archivo = CrearCsv(csv);

            // Act
            var resultado = await _service.ProcesarCargaCsvAsync(1, archivo);

            // Assert
            resultado.Registrados.Should().Be(1);
            resultado.ConstanciasGeneradas.Should().Be(1);
            resultado.Errores.Should().BeEmpty();
        }

        [Test]
        public async Task ProcesarCargaCsvAsync_FolioConFormatoInvalido_AgregaErrorEnFila()
        {
            // Arrange
            var plantilla = CrearPlantillaFake();
            _plantillaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(plantilla);
            SetupReposVacios();

            var csv = "folio,nombre,apellido1,apellido2,curso\nFOLIO-MAL,Ana,López,,Curso";
            var archivo = CrearCsv(csv);

            // Act
            var resultado = await _service.ProcesarCargaCsvAsync(1, archivo);

            // Assert
            resultado.Errores.Should().HaveCount(1);
            resultado.Errores[0].Fila.Should().Be(2);
            resultado.Errores[0].Motivo.Should().Contain("formato del folio");
        }

        [Test]
        public async Task ProcesarCargaCsvAsync_CamposObligatoriosVacios_AgregaErrorConCamposFaltantes()
        {
            // Arrange
            var plantilla = CrearPlantillaFake();
            _plantillaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(plantilla);
            SetupReposVacios();

            var csv = "folio,nombre,apellido1,apellido2,curso\n,,López,,"; // sin folio, nombre ni curso
            var archivo = CrearCsv(csv);

            // Act
            var resultado = await _service.ProcesarCargaCsvAsync(1, archivo);

            // Assert
            resultado.Errores.Should().HaveCount(1);
            resultado.Errores[0].Motivo.Should().Contain("Folio");
            resultado.Errores[0].Motivo.Should().Contain("Nombre");
            resultado.Errores[0].Motivo.Should().Contain("Curso");
        }

        [Test]
        public async Task ProcesarCargaCsvAsync_FolioDuplicadoEnMismoCsv_SegundaFilaVaAErrores()
        {
            // Arrange
            var plantilla = CrearPlantillaFake();
            _plantillaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(plantilla);
            SetupReposVacios();

            var csv = "folio,nombre,apellido1,apellido2,curso\n" +
                      "2020-1234-1,Ana,López,,Curso A\n" +
                      "2020-1234-1,Ana,López,,Curso A"; // duplicado mismo curso
            var archivo = CrearCsv(csv);

            // Act
            var resultado = await _service.ProcesarCargaCsvAsync(1, archivo);

            // Assert
            resultado.Errores.Should().HaveCount(1);
            resultado.Errores[0].Fila.Should().Be(3);
            resultado.Errores[0].Motivo.Should().Contain("duplicado");
        }

        [Test]
        public async Task ProcesarCargaCsvAsync_FolioExistenteConNombreDiferente_AgregaErrorDeConflicto()
        {
            // Arrange
            var plantilla = CrearPlantillaFake();
            _plantillaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(plantilla);

            var participanteExistente = new Participante("2020-1234-1", "Ana", "López", "García");
            _participanteRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Participante> { participanteExistente });
            _cursoRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Curso>());
            _constanciaRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Constancia>());

            var csv = "folio,nombre,apellido1,apellido2,curso\n2020-1234-1,Nombre Diferente,López,,Curso A";
            var archivo = CrearCsv(csv);

            // Act
            var resultado = await _service.ProcesarCargaCsvAsync(1, archivo);

            // Assert
            resultado.Errores.Should().HaveCount(1);
            resultado.Errores[0].Motivo.Should().Contain("Ana López García");
        }

        [Test]
        public async Task ProcesarCargaCsvAsync_ConstanciaDuplicadaEnBD_AgregaErrorDeConstanciaExistente()
        {
            // Arrange
            var plantilla = CrearPlantillaFake();
            _plantillaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(plantilla);

            var curso = new Curso("Curso A") { Id = 5 };
            var participante = new Participante("2020-1234-1", "Ana", "López", "");
            var constanciaExistente = new Constancia(participante, plantilla, curso)
            {
                FolioParticipante = "2020-1234-1",
                CursoId = 5
            };

            _participanteRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Participante> { participante });
            _cursoRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Curso> { curso });
            _constanciaRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Constancia> { constanciaExistente });

            var csv = "folio,nombre,apellido1,apellido2,curso\n2020-1234-1,Ana,López,,Curso A";
            var archivo = CrearCsv(csv);

            // Act
            var resultado = await _service.ProcesarCargaCsvAsync(1, archivo);

            // Assert
            resultado.Errores.Should().HaveCount(1);
            resultado.Errores[0].Motivo.Should().Contain("constancia registrada en el sistema");
        }

        [Test]
        public async Task ProcesarCargaCsvAsync_FolioExistenteEnBDMismoNombre_ReutilizaParticipanteSinCrearNuevo()
        {
            // Arrange
            var plantilla = CrearPlantillaFake();
            _plantillaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(plantilla);
            SetupReposVacios();

            var participanteExistente = new Participante("2020-1234-1", "Ana", "López", "");
            _participanteRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Participante> { participanteExistente });

            var csv = "folio,nombre,apellido1,apellido2,curso\n2020-1234-1,Ana,López,,Curso A";
            var archivo = CrearCsv(csv);

            // Act
            var resultado = await _service.ProcesarCargaCsvAsync(1, archivo);

            // Assert
            resultado.Registrados.Should().Be(0); // no agregó uno nuevo
            resultado.Errores.Should().BeEmpty();
            _participanteRepoMock.Verify(r => r.AddAsync(It.IsAny<Participante>()), Times.Never);
        }

        [Test]
        public async Task ProcesarCargaCsvAsync_SoloValidarTrue_NoLlamaAAddAsync()
        {
            // Arrange
            var plantilla = CrearPlantillaFake();
            _plantillaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(plantilla);
            SetupReposVacios();

            var csv = "folio,nombre,apellido1,apellido2,curso\n2020-1234-1,Ana,López,,Curso A";
            var archivo = CrearCsv(csv);

            // Act
            var resultado = await _service.ProcesarCargaCsvAsync(1, archivo, soloValidar: true);

            // Assert
            resultado.ConstanciasGeneradas.Should().Be(1);
            _participanteRepoMock.Verify(r => r.AddAsync(It.IsAny<Participante>()), Times.Never);
            _cursoRepoMock.Verify(r => r.AddAsync(It.IsAny<Curso>()), Times.Never);
            _constanciaRepoMock.Verify(r => r.AddAsync(It.IsAny<Constancia>()), Times.Never);
            _constanciaRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Test]
        public async Task ProcesarCargaCsvAsync_CsvSinFilasDeDatos_DevuelveCeroRegistrosYSinErrores()
        {
            // Arrange
            var plantilla = CrearPlantillaFake();
            _plantillaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(plantilla);
            SetupReposVacios();

            var csv = "folio,nombre,apellido1,apellido2,curso"; // solo header
            var archivo = CrearCsv(csv);

            // Act
            var resultado = await _service.ProcesarCargaCsvAsync(1, archivo);

            // Assert
            resultado.Registrados.Should().Be(0);
            resultado.ConstanciasGeneradas.Should().Be(0);
            resultado.Errores.Should().BeEmpty();
        }
    }
}