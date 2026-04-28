using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Siscomat.Core.Entities;

namespace Siscomat.Core.Interfaces
{
    public interface IGestorRepository
    {
        Task<Gestor?> GetByIdAsync(int id);
        Task<Gestor?> GetByCorreoAsync(string correo);
        Task<IEnumerable<Gestor>> GetAllAsync();
        Task AddAsync(Gestor gestor);
        Task UpdateAsync(Gestor gestor);
        Task DeleteAsync(int id);
        Task<int> SaveChangesAsync();
    }
}
