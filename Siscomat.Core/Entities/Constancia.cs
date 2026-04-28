using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siscomat.Core.Entities
{
    public class Constancia
    {
        public int Id { get; init; }
        public string FolioParticipante { get; init; }
        public int CursoId { get; init; }
        public int PlantillaId { get; init; }
        public Participante Participante { get; init; }
        public Curso Curso { get; init; }
        public Plantilla Plantilla { get; init; }
        public DateTime CreatedAt { get; init; }

        public Constancia() { }

        public Constancia(Participante participante, Plantilla plantilla, Curso curso)
        {
            Participante = participante;
            FolioParticipante = participante.Folio;

            Plantilla = plantilla;
            PlantillaId = plantilla.Id;

            Curso = curso;
            CursoId = curso.Id;

            CreatedAt = DateTime.UtcNow;
        }
    }
}
