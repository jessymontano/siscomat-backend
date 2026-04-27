using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Siscomat.Core.Entities;

namespace Siscomat.Core.Interfaces
{
    public interface IParticipanteRepository
    {
        Task<Participante?> GetByFolioAsync(string folio);
        Task<IEnumerable<Participante>> GetAllAsync();
        Task AddAsync(Participante participante);
        Task AddRangeAsync(IEnumerable<Participante> participantes);
        Task UpdateAsync(Participante participante);
        Task DeleteAsync(string folio);
        Task<int> SaveChangesAsync();
    }
}
