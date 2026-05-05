using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Siscomat.Core.Entities;
using Siscomat.Core.Interfaces;

namespace Siscomat.Services
{
    public class PublicService
    {
        private readonly IParticipanteRepository _participanteRepo;
        private readonly IConstanciaRepository _constanciaRepo;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public PublicService(
            IParticipanteRepository participanteRepo,
            IConstanciaRepository constanciaRepo,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _participanteRepo = participanteRepo;
            _constanciaRepo = constanciaRepo;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<Participante?> GetConstanciasByFolioAsync(string folio)
        {
            return await _participanteRepo.GetByFolioWithConstanciasAsync(folio);
        }

        public async Task<Constancia?> ValidarConstanciaAsync(Guid id)
        {
            return await _constanciaRepo.GetByIdWithDetailsAsync(id);
        }

        public async Task<byte[]?> GenerarPdfAsync(Guid id)
        {
            var constancia = await _constanciaRepo.GetByIdWithDetailsAsync(id);
            if (constancia == null) return null;

            var plantillaBytes = await File.ReadAllBytesAsync(constancia.Plantilla.Path);
            var plantillaBase64 = Convert.ToBase64String(plantillaBytes);

            var portalUrl = _configuration.GetValue<string>("FrontendSettings:PortalPublicoUrl");
            var urlValidacion = $"{portalUrl}/validar/{id}";

            var nombre = $"{constancia.Participante.Nombre} {constancia.Participante.Apellido1} {constancia.Participante.Apellido2}".Trim();

            var body = new
            {
                nombre_participante = nombre,
                nombre_curso = constancia.Curso.Nombre,
                url_validacion = urlValidacion,
                plantilla_base64 = plantillaBase64
            };

            var apiKey = _configuration.GetValue<string>("MicroservicioSettings:ApiKey");
            var client = _httpClientFactory.CreateClient("microservicio");
            client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

            var response = await client.PostAsJsonAsync("/api/v1/constancias/generar-individual", body);
            if (!response.IsSuccessStatusCode) return null;

            var resultado = await response.Content.ReadFromJsonAsync<GenerarPdfResponse>();
            if (resultado?.ArchivoBase64 == null) return null;

            return Convert.FromBase64String(resultado.ArchivoBase64);
        }

        private record GenerarPdfResponse(
            [property: JsonPropertyName("archivo_base64")]
            string ArchivoBase64
        );
    }
}