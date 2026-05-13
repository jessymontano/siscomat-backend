using Siscomat.Core.Entities;

namespace Siscomat.Core.Interfaces
{
    public interface IPublicService
    {
        Task<Participante?> GetConstanciasByFolioAsync(string folio);
        Task<Constancia?> ValidarConstanciaAsync(Guid id);
        Task<byte[]?> GenerarPdfAsync(Guid id);
    }
}