using Microsoft.AspNetCore.Mvc;

namespace HelpDeskTI.Web.Models
{
    public class SoftwareComputadora
    {
        public int IdSoftwareComputadora { get; set; }
        public int IdSoftware { get; set; }
        public int IdComputadora { get; set; }
        public DateTime FechaInstalacion { get; set; }
    }
}

