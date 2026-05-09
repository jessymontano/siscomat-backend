using System;
using System.Collections.Generic;
using System.Text;

namespace Siscomat.Core.DTOs
{
    public class LoginDTO
    {
        /// <summary>
        /// Correo del gestor. Este campo es obligatorio y se utiliza para identificar al usuario que intenta iniciar sesión. Debe ser una dirección de correo electrónico válida.
        /// </summary>
        /// <example>usuario@ejemplo.com</example>
        public string Correo { get; set; } = string.Empty;
        /// <summary>
        /// Contraseña del gestor. Este campo es obligatorio y se utiliza para autenticar al usuario.
        /// </summary>
        /// <example>ContraseñaSegura123!</example>
        public string Password { get; set; } = string.Empty;
    }
}
