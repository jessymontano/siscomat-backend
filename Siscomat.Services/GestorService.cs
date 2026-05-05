using Siscomat.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Siscomat.Services
{
    public class GestorService
    {
        protected readonly IGestorRepository _gestorRepository;

        public GestorService(IGestorRepository gestorRepository)
        {
            _gestorRepository = gestorRepository;
        }

        public async Task<IEnumerable<Core.Entities.Gestor>> GetAllAsync()
        {
                return await _gestorRepository.GetAllAsync();
        }

        public async Task<Core.Entities.Gestor?> GetByIdAsync(int id)
        {
            return await _gestorRepository.GetByIdAsync(id);
        }   

        public async Task<Core.Entities.Gestor?> GetByCorreoAsync(string correo)
        {
            return await _gestorRepository.GetByCorreoAsync(correo);
        }   

        public async Task AddAsync(Core.Entities.Gestor gestor)
        {
            if (!string.IsNullOrWhiteSpace(gestor.PasswordHash))
            {
                gestor.PasswordHash = BCrypt.Net.BCrypt.HashPassword(gestor.PasswordHash);
            }
            await _gestorRepository.AddAsync(gestor);
        }

        public async Task UpdateAsync(Core.Entities.Gestor gestor)
        {
            await _gestorRepository.UpdateAsync(gestor);
        }

        public async Task DeleteAsync(int id)
        {
            await _gestorRepository.DeleteAsync(id);
        }

        public async Task<int> SaveChangesAsync()
        {
                return await _gestorRepository.SaveChangesAsync();
        }
    }
}
