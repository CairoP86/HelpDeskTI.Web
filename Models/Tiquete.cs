using System.ComponentModel.DataAnnotations;

namespace HelpDeskTI.Web.Models
{
    public class Tiquete
    {
        [Key]
        public int IdTiquete { get; set; }

        [Required]
        [StringLength(150)]
        public string Titulo { get; set; }

        [Required]
        public string Descripcion { get; set; }

        [StringLength(20)]
        public string Estado { get; set; } = "Abierto";

        [Required]
        [StringLength(20)]
        public string Prioridad { get; set; } // Baja | Media | Alta

        public DateTime FechaCreacion { get; set; }

        public DateTime? FechaCierre { get; set; }

        [Required]
        public string CedulaCreador { get; set; }

        public string? CedulaTecnico { get; set; }
    }
}
