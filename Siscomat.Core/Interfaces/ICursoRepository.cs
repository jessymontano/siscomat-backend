using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Siscomat.Core.Entities;

namespace Siscomat.Core.Interfaces
{
    public interface ICursoRepository
    {
        Task<Curso?> GetByIdAsync(int id);
        Task<Curso?> GetCursoByNombre(string nombre);
        Task<IEnumerable<Curso>> GetAllAsync();
        Task AddAsync(Curso curso);
        Task AddRangeAsync(IEnumerable<Curso> cursos);
        Task<int> SaveChangesAsync();
    }
}
