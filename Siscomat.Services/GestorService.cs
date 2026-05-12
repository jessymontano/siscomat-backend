using Siscomat.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Siscomat.Services
{
    public class GestorService : IGestorService
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

        public async Task UpdateAsync(int id,Core.Entities.Gestor gestor)
        {
            var gestorOriginal = await _gestorRepository.GetByIdAsync(id);

            if (gestorOriginal == null) return; 

            if (!string.IsNullOrWhiteSpace(gestor.Nombre))
                gestorOriginal.Nombre = gestor.Nombre;

            if (!string.IsNullOrWhiteSpace(gestor.Apellido1))
                gestorOriginal.Apellido1 = gestor.Apellido1;

            if (!string.IsNullOrWhiteSpace(gestor.Apellido2))
                gestorOriginal.Apellido2 = gestor.Apellido2;

            if (!string.IsNullOrWhiteSpace(gestor.Correo))
                gestorOriginal.Correo = gestor.Correo;

            if (!string.IsNullOrWhiteSpace(gestor.PasswordHash))
                gestorOriginal.PasswordHash = BCrypt.Net.BCrypt.HashPassword(gestor.PasswordHash);

            if (gestor.EsAdmin != gestorOriginal.EsAdmin)
                gestorOriginal.EsAdmin = gestor.EsAdmin;

            await _gestorRepository.UpdateAsync(gestorOriginal);
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
