using Microsoft.EntityFrameworkCore;
using Siscomat.Core.Entities;
using Siscomat.Core.Interfaces;

namespace Siscomat.Repositories
{
    public class CursoRepository : ICursoRepository
    {
        private readonly ApplicationDbContext _db;

        public CursoRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Curso?> GetByIdAsync(int id)
        {
            return await _db.Cursos.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Curso?> GetCursoByNombre(string nombre)
        {
            return await _db.Cursos.FirstOrDefaultAsync(c => c.Nombre.ToLower() == nombre.ToLower());
        }

        public async Task<IEnumerable<Curso>> GetAllAsync()
        {
            return await _db.Cursos.ToListAsync();
        }

        public async Task AddAsync(Curso curso)
        {
            await _db.Cursos.AddAsync(curso);
        }

        public async Task AddRangeAsync(IEnumerable<Curso> cursos)
        {
            await _db.Cursos.AddRangeAsync(cursos);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _db.SaveChangesAsync();
        }
    }
}