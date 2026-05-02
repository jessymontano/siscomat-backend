using Siscomat.Core.Entities;
using Siscomat.Core.Interfaces;

namespace Siscomat.Services
{
    public class PublicService
    {
        private readonly IParticipanteRepository _participanteRepo;
        private readonly IConstanciaRepository _constanciaRepo;

        public PublicService(IParticipanteRepository participanteRepo, IConstanciaRepository constanciaRepo)
        {
            _participanteRepo = participanteRepo;
            _constanciaRepo = constanciaRepo;
        }

        public async Task<Participante?> GetConstanciasByFolioAsync(string folio)
        {
            return await _participanteRepo.GetByFolioWithConstanciasAsync(folio);
        }

        public async Task<Constancia?> ValidarConstanciaAsync(Guid id)
        {
            return await _constanciaRepo.GetByIdWithDetailsAsync(id);
        }
    }
}