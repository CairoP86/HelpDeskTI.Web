using System.ComponentModel.DataAnnotations;

namespace HelpDeskTI.Web.Models
{
    public class Computadora
    {
        public int IdComputadora { get; set; }

        [Required]
        public string CodigoActivo { get; set; } = "";

        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public string? Serie { get; set; }

        // 🔑 FK real en la BD
        public int? IdEmpleado { get; set; }

        // 🔹 SOLO PARA MOSTRAR (NO EXISTE EN SQL)
        public string? CedulaUsuario { get; set; }
        public string? NombreUsuarioAsignado { get; set; }
    }
}
