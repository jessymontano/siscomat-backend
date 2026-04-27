using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Siscomat.Core.Entities;

namespace Siscomat.Core.Interfaces
{
    public interface IPlantillaRepository
    {
        Task<Plantilla?> GetByIdAsync(int id);
        Task<IEnumerable<Plantilla>> GetAllAsync();
        Task AddAsync(Plantilla plantilla);
        Task DeleteAsync(int id);
        Task<int> SaveChangesAsync();
    }
}
