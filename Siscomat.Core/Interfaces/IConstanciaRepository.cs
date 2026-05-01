using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Siscomat.Core.Entities;

namespace Siscomat.Core.Interfaces
{
    public interface IConstanciaRepository
    {
        Task<Constancia?> GetByIdAsync(Guid id);
        Task<IEnumerable<Constancia>> GetAllAsync();
        Task<IEnumerable<Constancia>> GetByParticipanteFolioAsync(string folio);
        Task<IEnumerable<Constancia>> GetByCursoIdAsync(int cursoId);
        Task AddAsync(Constancia constancia);
        Task AddRangeAsync(IEnumerable<Constancia> constancias);
        Task<int> SaveChangesAsync();
        Task<Constancia?> GetByIdWithDetailsAsync(Guid id); // Método específico para obtener una constancia con sus detalles, requerido por PublicController
    }
}
