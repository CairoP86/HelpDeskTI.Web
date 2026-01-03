using System.ComponentModel.DataAnnotations;

namespace HelpDeskTI.Web.Models
{
    public class Computadora
    {
        [Key]
        public int IdComputadora { get; set; }

        [Required]
        public string CodigoActivo { get; set; }

        [Required]
        public string Marca { get; set; }

        [Required]
        public string Modelo { get; set; }

        public string? Serie { get; set; }

        public int? IdEmpleado { get; set; }
    }
}
