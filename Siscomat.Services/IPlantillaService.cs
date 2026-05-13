using Microsoft.AspNetCore.Http;
using Siscomat.Core.Entities;

namespace Siscomat.Core.Interfaces
{
    public interface IPlantillaService
    {
        Task<IEnumerable<Plantilla>> GetAllAsync();
        Task<Plantilla?> GetByIdAsync(int id);
        Task<Plantilla> SubirAsync(string nombre, IFormFile archivo);
        Task<(bool eliminado, bool enUso)> EliminarAsync(int id);
        Task<(byte[] bytes, string path)?> GetArchivoAsync(int id);
    }
}