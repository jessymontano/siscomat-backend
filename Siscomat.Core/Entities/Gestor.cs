using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siscomat.Core.Entities
{
    public class Gestor
    {
        public int Id { get; init; }
        public string Nombre { get; private set; }
        public string Apellido1 { get; private set; }
        public string Apellido2 { get; private set; }
        public string Correo { get; private set; }
        public string PasswordHash { get; private set; }
        public bool EsAdmin { get; set; }

        public Gestor() { }

        public Gestor(string nombre, string apellido1, string apellido2, string correo, string passwordHash, bool esAdmin)
        {
            Nombre = nombre;
            Apellido1 = apellido1;
            Apellido2 = apellido2;
            Correo = correo;
            PasswordHash = passwordHash;
            EsAdmin = esAdmin;
        }

        public void UpdateInfo(string nombre, string apellido1, string apellido2)
        {
            Nombre = nombre;
            Apellido1 = apellido1;
            Apellido2 = apellido2;
        }

        public void UpdateCorreo(string correo)
        {
            Correo = correo;
        }

        public void UpdatePasswordHash(string passwordHash)
        {
            PasswordHash = passwordHash;
        }

        public void UpdateRole(bool esAdmin)
        {
            EsAdmin = esAdmin;
        }
    }
}
