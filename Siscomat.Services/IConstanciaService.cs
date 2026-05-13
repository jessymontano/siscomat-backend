using Microsoft.AspNetCore.Http;
using Siscomat.Services;

namespace Siscomat.Core.Interfaces
{
    public interface IConstanciaService
    {
        Task<PythonPreviewResponse> PrevisualizarAsync(PrevisualizarRequest req);
        Task<CargaConstanciasResponse> ProcesarCargaCsvAsync(int plantillaId, IFormFile archivo, bool soloValidar = false);
    }
}