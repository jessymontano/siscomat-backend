using Microsoft.EntityFrameworkCore;
using Siscomat.Core.Entities;
using Siscomat.Core.Interfaces;
using System;

namespace Siscomat.Repositories
{
    public class ConstanciaRepository : IConstanciaRepository
    {
        private readonly ApplicationDbContext _db;

        public ConstanciaRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Constancia?> GetByIdAsync(Guid id)
        {
            return await _db.Constancias.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Constancia?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _db.Constancias
                .Include(c => c.Participante)
                .Include(c => c.Curso)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Constancia>> GetAllAsync()
        {
            return await _db.Constancias.ToListAsync();
        }

        public async Task<IEnumerable<Constancia>> GetByParticipanteFolioAsync(string folio)
        {
            return await _db.Constancias
                .Where(c => c.FolioParticipante == folio)
                .ToListAsync();
        }

        public async Task<IEnumerable<Constancia>> GetByCursoIdAsync(int cursoId)
        {
            return await _db.Constancias
                .Where(c => c.CursoId == cursoId)
                .ToListAsync();
        }

        public async Task AddAsync(Constancia constancia)
        {
            await _db.Constancias.AddAsync(constancia);
        }

        public async Task AddRangeAsync(IEnumerable<Constancia> constancias)
        {
            await _db.Constancias.AddRangeAsync(constancias);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _db.SaveChangesAsync();
        }
    }
}