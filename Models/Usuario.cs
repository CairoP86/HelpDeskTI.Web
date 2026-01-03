using System.ComponentModel.DataAnnotations;

namespace HelpDeskTI.Web.Models
{
    public class Usuario
    {
        [Key]
        [StringLength(50)]
        public string Cedula { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Required]
        [StringLength(100)]
        public string Apellido1 { get; set; }

        [StringLength(100)]
        public string? Apellido2 { get; set; }

        [Required]
        [StringLength(20)]
        public string Rol { get; set; }   // Admin | Tecnico | Usuario

        [Required]
        public string ClaveHash { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }

        public bool DebeCambiarClave { get; set; }

        public DateTime? FechaCambioClave { get; set; }
    }
}
