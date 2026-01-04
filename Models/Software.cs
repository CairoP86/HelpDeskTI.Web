using System.ComponentModel.DataAnnotations;

namespace HelpDeskTI.Web.Models
{
    public class Software
    {
        public int IdSoftware { get; set; }

        [Required]
        public string Nombre { get; set; } = "";

        public string? Version { get; set; }

        [Required]
        public string TipoLicencia { get; set; } = "";

        public int StockLicencias { get; set; }

        public int LicenciasInstaladas { get; set; }
    }
}
