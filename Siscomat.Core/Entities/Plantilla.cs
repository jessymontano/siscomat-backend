using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siscomat.Core.Entities
{
    public class Plantilla
    {

        public int Id { get; init; }
        public string Nombre { get; private set; }
        public string Path { get; private set; }
        public DateTime CreatedAt { get; init; }

        public ICollection<Constancia> Constancias { get; private set; } = new List<Constancia>();

        public Plantilla() { }

        public Plantilla(string nombre, string path)
        {
            Nombre = nombre;
            Path = path;
            CreatedAt = DateTime.UtcNow;
        }

        // por si se ocupa cambiar el nombre de la plantilla
        public void UpdateNombre(string nombre)
        {
            Nombre = nombre;
        }

        public void AddConstancia(Constancia constancia)
        {
            Constancias.Add(constancia);
        }
    }
}
