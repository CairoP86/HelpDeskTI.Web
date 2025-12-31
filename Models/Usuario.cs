using Microsoft.AspNetCore.Mvc;

namespace HelpDeskTI.Web.Models
{
    public class Usuario
    {
        public string Cedula { get; set; }
        public string Nombre { get; set; }
        public string Apellido1 { get; set; }
        public string Apellido2 { get; set; }
        public string Rol { get; set; }
    }
}

