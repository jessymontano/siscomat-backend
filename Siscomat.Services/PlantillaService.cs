using Siscomat.Core.Entities;
using Siscomat.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;

namespace Siscomat.Services
{
    public record ValidacionPlantillaResponse(bool es_valida, List<string> placeholders_encontrados, List<string> placeholders_faltantes);

    public class PlantillaInvalidaException : Exception
    {
        public ValidacionPlantillaResponse Detalles { get; }
        public PlantillaInvalidaException(ValidacionPlantillaResponse detalles) : base("La plantilla no pasó la validación.")
        {
            Detalles = detalles;
        }
    }

    public class PlantillaService : IPlantillaService
    {
        private readonly IPlantillaRepository _plantillaRepo;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _storagePath;
        private readonly string _pythonApiUrl;
        private readonly string _pythonApiKey;

        public PlantillaService(IPlantillaRepository plantillaRepo, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _plantillaRepo = plantillaRepo;
            _httpClientFactory = httpClientFactory;
            _storagePath = configuration.GetValue<string>("Storage:PlantillasPath") ?? "plantillas";
            _pythonApiUrl = configuration.GetValue<string>("MicroservicioSettings:Url") ?? throw new ArgumentNullException("MicroservicioSettings:Url no está configurado");
            _pythonApiKey = configuration.GetValue<string>("MicroservicioSettings:ApiKey") ?? throw new ArgumentNullException("MicroservicioSettings:ApiKey no está configurado");
        }

        public async Task<IEnumerable<Plantilla>> GetAllAsync()
        {
            return await _plantillaRepo.GetAllAsync();
        }

        public async Task<Plantilla?> GetByIdAsync(int id)
        {
            return await _plantillaRepo.GetByIdAsync(id);
        }

        public async Task<Plantilla> SubirAsync(string nombre, IFormFile archivo)
        {
            using var memoryStream = new MemoryStream();
            await archivo.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();
            var base64 = Convert.ToBase64String(bytes);

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-API-Key", _pythonApiKey);

            var payload = new { plantilla_base64 = base64 };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{_pythonApiUrl}/api/v1/constancias/validar-plantilla", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Error al comunicarse con el microservicio de validación.");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var validacion = JsonSerializer.Deserialize<ValidacionPlantillaResponse>(
                jsonResponse, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (validacion != null && !validacion.es_valida)
            {
                throw new PlantillaInvalidaException(validacion);
            }

            Directory.CreateDirectory(_storagePath);

            var nombreArchivo = $"{Guid.NewGuid()}.pdf";
            var path = Path.Combine(_storagePath, nombreArchivo);

            await File.WriteAllBytesAsync(path, bytes);

            var plantilla = new Plantilla(nombre, path);
            await _plantillaRepo.AddAsync(plantilla);
            await _plantillaRepo.SaveChangesAsync();

            return plantilla;
        }

        public async Task<(bool eliminado, bool enUso)> EliminarAsync(int id)
        {
            var plantilla = await _plantillaRepo.GetByIdAsync(id);
            if (plantilla == null) return (false, false);

            if (plantilla.Constancias.Any()) return (true, true);

            if (File.Exists(plantilla.Path))
                File.Delete(plantilla.Path);

            await _plantillaRepo.DeleteAsync(id);
            await _plantillaRepo.SaveChangesAsync();

            return (true, false);
        }

        public async Task<(byte[] bytes, string path)?> GetArchivoAsync(int id)
        {
            var plantilla = await _plantillaRepo.GetByIdAsync(id);
            if (plantilla == null || !File.Exists(plantilla.Path)) return null;

            var bytes = await File.ReadAllBytesAsync(plantilla.Path);
            return (bytes, plantilla.Path);
        }
    }
}