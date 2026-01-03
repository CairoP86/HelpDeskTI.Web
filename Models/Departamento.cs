using System.ComponentModel.DataAnnotations;

namespace HelpDeskTI.Web.Models
{
    public class Departamento
    {
        [Key]
        public int IdDepartamento { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }
    }
}
