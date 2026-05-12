using Siscomat.Core.Entities;

namespace Siscomat.Services
{
    public interface IGestorService
    {
        Task AddAsync(Gestor gestor);
        Task DeleteAsync(int id);
        Task<IEnumerable<Gestor>> GetAllAsync();
        Task<Gestor?> GetByCorreoAsync(string correo);
        Task<Gestor?> GetByIdAsync(int id);
        Task<int> SaveChangesAsync();
        Task UpdateAsync(int id, Gestor gestor);
    }
}