using Microsoft.EntityFrameworkCore;
using Siscomat.Core.Entities;
using Siscomat.Core.Interfaces;
using System.Linq;

namespace Siscomat.Repositories
{
    public class PlantillaRepository : IPlantillaRepository
    {
        private readonly ApplicationDbContext _db;

        public PlantillaRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Plantilla?> GetByIdAsync(int id)
        {
            return await _db.Plantillas
                .Include(p => p.Constancias)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Plantilla>> GetAllAsync()
        {
            return await _db.Plantillas
                .Include(p => p.Constancias)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(Plantilla plantilla)
        {
            await _db.Plantillas.AddAsync(plantilla);
        }

        public async Task DeleteAsync(int id)
        {
            var plantilla = await _db.Plantillas.FirstOrDefaultAsync(p => p.Id == id);
            if (plantilla != null)
                _db.Plantillas.Remove(plantilla);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _db.SaveChangesAsync();
        }
    }
}