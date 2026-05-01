using Microsoft.EntityFrameworkCore;
using Siscomat.Core.Entities;
using Siscomat.Core.Interfaces;

namespace Siscomat.Repositories
{
    public class ParticipanteRepository : IParticipanteRepository
    {
        private readonly ApplicationDbContext _db;

        public ParticipanteRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Participante?> GetByFolioAsync(string folio)
        {
            return await _db.Participantes.FirstOrDefaultAsync(p => p.Folio == folio);
        }

        public async Task<Participante?> GetByFolioWithConstanciasAsync(string folio)
        {
            return await _db.Participantes
                .Include(p => p.Constancias)
                    .ThenInclude(c => c.Curso)
                .FirstOrDefaultAsync(p => p.Folio == folio);
        }

        public async Task<IEnumerable<Participante>> GetAllAsync()
        {
            return await _db.Participantes.ToListAsync();
        }

        public async Task AddAsync(Participante participante)
        {
            await _db.Participantes.AddAsync(participante);
        }

        public async Task AddRangeAsync(IEnumerable<Participante> participantes)
        {
            await _db.Participantes.AddRangeAsync(participantes);
        }

        public async Task UpdateAsync(Participante participante)
        {
            _db.Participantes.Update(participante);
        }

        public async Task DeleteAsync(string folio)
        {
            var participante = await GetByFolioAsync(folio);
            if (participante != null)
                _db.Participantes.Remove(participante);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _db.SaveChangesAsync();
        }
    }
}