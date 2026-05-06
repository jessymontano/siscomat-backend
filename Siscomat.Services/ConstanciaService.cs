using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Siscomat.Core.Entities;
using Siscomat.Core.Interfaces;
using Siscomat.Repositories;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Siscomat.Services
{
    public class PrevisualizarRequest
    {
        public string folio { get; set; } = string.Empty;
        public string nombre_participante { get; set; } = string.Empty;
        public string nombre_curso { get; set; } = string.Empty;
        public int plantilla_id { get; set; }
    }

    public class PythonPreviewResponse
    {
        public string estado { get; set; } = string.Empty;
        public string mensaje { get; set; } = string.Empty;
        public string archivo_base64 { get; set; } = string.Empty;
    }

    public class ParticipanteCsvDto
    {
        [Name("folio")]
        public string Folio { get; set; } = string.Empty;
        [Name("nombre")]
        public string Nombre { get; set; } = string.Empty;
        [Name("apellido1")]
        public string Apellido1 { get; set; } = string.Empty;
        [Name("apellido2")]
        public string Apellido2 { get; set; } = string.Empty;
        [Name("curso")]
        public string Curso { get; set; } = string.Empty;
    }

    public record ErrorFila(int Fila, string Motivo);
    public record CargaConstanciasResponse(int Registrados, int ConstanciasGeneradas, List<ErrorFila> Errores);

    public class ConstanciaService
    {
        private readonly IConstanciaRepository _constanciaRepo;
        private readonly IParticipanteRepository _participanteRepo;
        private readonly IPlantillaRepository _plantillaRepo;
        private readonly ICursoRepository _cursoRepo; 
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _pythonApiUrl;
        private readonly string _pythonApiKey;

        private static readonly Regex FolioRegex = new Regex(@"^\d{4}-\d{4}-\d+$", RegexOptions.Compiled);
        
        private readonly int _limiteNombreParticipante;
        private readonly int _limiteApellidoParticipante;
        private readonly int _limiteNombreCurso;

        public ConstanciaService(
            IConstanciaRepository constanciaRepo,
            IParticipanteRepository participanteRepo,
            IPlantillaRepository plantillaRepo,
            ICursoRepository cursoRepo, 
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ApplicationDbContext dbContext)
        {
            _constanciaRepo = constanciaRepo;
            _participanteRepo = participanteRepo;
            _plantillaRepo = plantillaRepo;
            _cursoRepo = cursoRepo;
            _httpClientFactory = httpClientFactory;
            _pythonApiUrl = config.GetValue<string>("MicroservicioSettings:Url") ?? throw new ArgumentNullException("Url no configurada");
            _pythonApiKey = config.GetValue<string>("MicroservicioSettings:ApiKey") ?? throw new ArgumentNullException("ApiKey no configurada");

            var participanteModel = dbContext.Model.FindEntityType(typeof(Participante));
            _limiteNombreParticipante = participanteModel?.FindProperty(nameof(Participante.Nombre))?.GetMaxLength() ?? 150;
            _limiteApellidoParticipante = participanteModel?.FindProperty(nameof(Participante.Apellido1))?.GetMaxLength() ?? 150;

            var cursoModel = dbContext.Model.FindEntityType(typeof(Curso));
            _limiteNombreCurso = cursoModel?.FindProperty(nameof(Curso.Nombre))?.GetMaxLength() ?? 150;
        }

        public async Task<PythonPreviewResponse> PrevisualizarAsync(PrevisualizarRequest req)
        {
            var plantilla = await _plantillaRepo.GetByIdAsync(req.plantilla_id);
            if (plantilla == null || !File.Exists(plantilla.Path))
                throw new FileNotFoundException("La plantilla seleccionada no existe en el servidor.");

            var bytes = await File.ReadAllBytesAsync(plantilla.Path);
            var plantillaBase64 = Convert.ToBase64String(bytes);

            var pythonPayload = new
            {
                nombre_curso = req.nombre_curso,
                nombre_participante = req.nombre_participante,
                plantilla_base64 = plantillaBase64
            };

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-API-Key", _pythonApiKey);

            var content = new StringContent(JsonSerializer.Serialize(pythonPayload), Encoding.UTF8, "application/json");
            
            var baseUrl = _pythonApiUrl.TrimEnd('/');
            var response = await client.PostAsync($"{baseUrl}/api/v1/constancias/previsualizar", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error en el generador: {errorContent}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PythonPreviewResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        public async Task<CargaConstanciasResponse> ProcesarCargaCsvAsync(int plantillaId, IFormFile archivo, bool soloValidar = false)
        {
            var plantilla = await _plantillaRepo.GetByIdAsync(plantillaId);
            if (plantilla == null) throw new FileNotFoundException("La plantilla no existe.");

            var errores = new List<ErrorFila>();
            int nuevosRegistrados = 0;
            int constanciasGeneradas = 0;

            using var stream = archivo.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null,
                PrepareHeaderForMatch = args => args.Header.ToLower() 
            });

            var records = csv.GetRecords<ParticipanteCsvDto>().ToList();

            var cursosExistentes = await _cursoRepo.GetAllAsync();
            var dictCursos = cursosExistentes.GroupBy(c => c.Nombre.Trim().ToLower()).ToDictionary(g => g.Key, g => g.First());

            var participantesExistentes = await _participanteRepo.GetAllAsync();
            var dictParticipantes = participantesExistentes.GroupBy(p => p.Folio.Trim()).ToDictionary(g => g.Key, g => g.First());

            var constanciasBD = await _constanciaRepo.GetAllAsync();
            
            var registrosEnExcel = new Dictionary<(string, string), int>();
            var constanciasNuevasBatch = new List<Constancia>();

            int numeroFila = 2; 

            foreach (var rec in records)
            {
                try
                {
                    var folioLimpio = rec.Folio?.Trim() ?? "";
                    var nombreLimpio = rec.Nombre?.Trim() ?? "";
                    var apellido1Limpio = rec.Apellido1?.Trim() ?? "";
                    var apellido2Limpio = rec.Apellido2?.Trim() ?? "";
                    var cursoLimpio = rec.Curso?.Trim() ?? "";

                    var camposFaltantes = new List<string>();
                    if (string.IsNullOrWhiteSpace(folioLimpio)) camposFaltantes.Add("Folio");
                    if (string.IsNullOrWhiteSpace(nombreLimpio)) camposFaltantes.Add("Nombre");
                    if (string.IsNullOrWhiteSpace(cursoLimpio)) camposFaltantes.Add("Curso");

                    if (camposFaltantes.Any())
                    {
                        errores.Add(new ErrorFila(numeroFila, $"Faltan datos obligatorios: <b>{string.Join(", ", camposFaltantes)}</b>."));
                        numeroFila++;
                        continue;
                    }

                    if (!FolioRegex.IsMatch(folioLimpio))
                    {
                        errores.Add(new ErrorFila(numeroFila, $"El formato del folio <b>{folioLimpio}</b> es inválido. Debe seguir el formato AAAA-NNNN-NN."));
                        numeroFila++;
                        continue;
                    }

                    if (nombreLimpio.Length > _limiteNombreParticipante || 
                        apellido1Limpio.Length > _limiteApellidoParticipante || 
                        apellido2Limpio.Length > _limiteApellidoParticipante)
                    {
                        errores.Add(new ErrorFila(numeroFila, $"El nombre o apellidos superan el límite máximo de {_limiteNombreParticipante} caracteres."));
                        numeroFila++;
                        continue;
                    }

                    if (cursoLimpio.Length > _limiteNombreCurso)
                    {
                        errores.Add(new ErrorFila(numeroFila, $"El nombre del curso supera el límite máximo de {_limiteNombreCurso} caracteres."));
                        numeroFila++;
                        continue;
                    }

                    var nombreCompleto = $"{nombreLimpio} {apellido1Limpio} {apellido2Limpio}".Trim();
                    var cursoKey = cursoLimpio.ToLower();

                    if (registrosEnExcel.TryGetValue((folioLimpio, cursoKey), out int filaAnterior))
                    {
                        errores.Add(new ErrorFila(numeroFila, $"El participante con folio <b>{folioLimpio}</b> tiene un <b>registro duplicado</b> en este mismo archivo CSV (previamente declarado en la fila <b>{filaAnterior}</b>) para el curso <b>{cursoLimpio}</b>."));
                        numeroFila++;
                        continue;
                    }

                    if (!dictParticipantes.TryGetValue(folioLimpio, out var participante))
                    {
                        participante = new Participante(folioLimpio, nombreLimpio, apellido1Limpio, apellido2Limpio);
                        if (!soloValidar) await _participanteRepo.AddAsync(participante);
                        dictParticipantes[folioLimpio] = participante; 
                        nuevosRegistrados++;
                    }
                    else
                    {
                        if (!participante.Nombre.Equals(nombreLimpio, StringComparison.OrdinalIgnoreCase))
                        {
                            var nombreRealBD = $"{participante.Nombre} {participante.Apellido1} {participante.Apellido2}".Trim();
                            errores.Add(new ErrorFila(numeroFila, $"El folio <b>{folioLimpio}</b> ya fue registrado con el nombre <b>{nombreRealBD}</b>, el cual difiere del nombre en este archivo: <b>{nombreCompleto}</b>."));
                            numeroFila++;
                            continue;
                        }
                    }

                    if (!dictCursos.TryGetValue(cursoKey, out var curso))
                    {
                        curso = new Curso(cursoLimpio);
                        if (!soloValidar) await _cursoRepo.AddAsync(curso);
                        dictCursos[cursoKey] = curso; 
                    }

                    bool yaTieneEnBD = curso.Id != 0 && constanciasBD.Any(c => c.FolioParticipante == folioLimpio && c.CursoId == curso.Id);
                    if (yaTieneEnBD)
                    {
                        errores.Add(new ErrorFila(numeroFila, $"El participante con folio <b>{folioLimpio}</b> ya tiene una <b>constancia registrada en el sistema</b> para el curso <b>{cursoLimpio}</b>."));
                        numeroFila++;
                        continue;
                    }

                    registrosEnExcel.Add((folioLimpio, cursoKey), numeroFila);
                    
                    var nuevaConstancia = new Constancia(participante, plantilla, curso);
                    if (!soloValidar) await _constanciaRepo.AddAsync(nuevaConstancia);
                    constanciasNuevasBatch.Add(nuevaConstancia); 

                    constanciasGeneradas++;
                }
                catch (Exception ex)
                {
                    errores.Add(new ErrorFila(numeroFila, $"Error inesperado: {ex.Message}"));
                }

                numeroFila++;
            }

            if (!soloValidar)
            {
                await _constanciaRepo.SaveChangesAsync();
            }

            return new CargaConstanciasResponse(nuevosRegistrados, constanciasGeneradas, errores);
        }
    }
}