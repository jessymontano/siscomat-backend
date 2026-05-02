using Siscomat.Core.Entities;
using Siscomat.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Siscomat.Services
{
    public class PlantillaService
    {
        private readonly IPlantillaRepository _plantillaRepo;
        private readonly string _storagePath;

        public PlantillaService(IPlantillaRepository plantillaRepo, IConfiguration configuration)
        {
            _plantillaRepo = plantillaRepo;
            _storagePath = configuration.GetValue<string>("Storage:PlantillasPath") ?? "plantillas";
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
            Directory.CreateDirectory(_storagePath);

            var nombreArchivo = $"{Guid.NewGuid()}.pdf";
            var path = Path.Combine(_storagePath, nombreArchivo);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            var plantilla = new Plantilla(nombre, path);
            await _plantillaRepo.AddAsync(plantilla);
            await _plantillaRepo.SaveChangesAsync();

            return plantilla;
        }

        public async Task<bool> EliminarAsync(int id)
        {
            var plantilla = await _plantillaRepo.GetByIdAsync(id);
            if (plantilla == null) return false;

            if (File.Exists(plantilla.Path))
                File.Delete(plantilla.Path);

            await _plantillaRepo.DeleteAsync(id);
            await _plantillaRepo.SaveChangesAsync();

            return true;
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