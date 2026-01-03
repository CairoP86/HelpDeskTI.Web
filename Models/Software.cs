using System.ComponentModel.DataAnnotations;

namespace HelpDeskTI.Web.Models
{
    public class Software
    {
        [Key]
        public int IdSoftware { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; }

        [Required]
        [StringLength(50)]
        public string Version { get; set; }

        [Required]
        [StringLength(50)]
        public string TipoLicencia { get; set; }

        public int StockLicencias { get; set; }

        public int LicenciasInstaladas { get; set; }
    }
}
