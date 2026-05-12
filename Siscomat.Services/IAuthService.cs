using Siscomat.Core.DTOs;
using Siscomat.Core.Entities;

namespace Siscomat.Services
{
    public interface IAuthService
    {
        Task<Gestor?> LoginAsync(LoginDTO loginDto);
    }
}