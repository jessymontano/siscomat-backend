using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siscomat.Core.Entities
{
    public class Participante
    {
        public string Folio { get; init; }
        public string Nombre { get; private set; }
        public string Apellido1 { get; private set; }
        public string Apellido2 { get; private set; }
        public DateTime CreatedAt { get; init; }
        public ICollection<Curso> Cursos { get; private set; } = new List<Curso>();
        public ICollection<Constancia> Constancias { get; private set; } = new List<Constancia>();

        public Participante() { }

        public Participante(string folio, string nombre, string apellido1, string apellido2)
        {
            Folio = folio;
            Nombre = nombre;
            Apellido1 = apellido1;
            Apellido2 = apellido2;
            CreatedAt = DateTime.UtcNow;
        }

        public void UpdateInfo(string nombre, string apellido1, string apellido2)
        {
            Nombre = nombre;
            Apellido1 = apellido1;
            Apellido2 = apellido2;
        }

        public void AddCurso(Curso curso)
        {
            Cursos.Add(curso);
        }

        public void AddConstancia(Constancia constancia)
        {
            Constancias.Add(constancia);
        }
    }
}
