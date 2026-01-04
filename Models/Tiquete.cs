using System;

namespace HelpDeskTI.Web.Models
{
    public class Tiquete
    {
        public int IdTiquete { get; set; }

        public string Titulo { get; set; } = null!;
        public string Descripcion { get; set; } = null!;

        public string Estado { get; set; } = "Abierto";
        public string? Prioridad { get; set; }

        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaCierre { get; set; }

        public string CedulaCreador { get; set; } = null!;
        public string? CedulaTecnico { get; set; }

        // SOLO PARA MOSTRAR
        public string? NombreCreador { get; set; }
        public string? NombreTecnico { get; set; }
    }
}
