using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siscomat.Core.Entities
{
    public class Curso
    {
        public int Id { get; init; }
        public string Nombre { get; private set; }
        public ICollection<Constancia> Constancias { get; private set; } = new List<Constancia>();

        public Curso() { }

        public Curso(string nombre)
        {
            Nombre = nombre;
        }

        public void UpdateInfo(string nombre)
        {
            Nombre = nombre;
        }

        public void AddConstancia(Constancia constancia)
        {
            Constancias.Add(constancia);
        }
    }
}
