using System;
using System.Collections.Generic;
using System.Text;
using Siscomat.Core.DTOs;
using Siscomat.Core.Entities;
using Siscomat.Core.Interfaces;

namespace Siscomat.Services
{
    public class AuthService : IAuthService
    {
        protected readonly IGestorRepository _gestorRepository;

        public AuthService(IGestorRepository gestorRepository)
        {
            _gestorRepository = gestorRepository;
        }

        public async Task<Gestor?> LoginAsync(LoginDTO loginDto)
        {
            var gestor = await _gestorRepository.GetByCorreoAsync(loginDto.Correo);
            if (gestor == null)
            {
                return null;
            }
            bool isValidPassword = BCrypt.Net.BCrypt.Verify(loginDto.Password, gestor.PasswordHash);
            if (!isValidPassword)
            {
                return null;
            }
            return gestor;
        }
    }
}
