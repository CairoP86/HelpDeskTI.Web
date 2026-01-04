using System;
using System.ComponentModel.DataAnnotations;

namespace HelpDeskTI.Web.Models
{
    public class Usuario
    {
        public string Cedula { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Apellido1 { get; set; } = null!;
        public string? Apellido2 { get; set; }

        public string Rol { get; set; } = null!;
        public bool Activo { get; set; }

        // Relación con Departamento
        public int? IdDepartamento { get; set; }

        // SOLO PARA MOSTRAR
        public string? NombreDepartamento { get; set; }

        // SOLO PARA MOSTRAR (Computadora asignada)
        public string? CodigoComputadora { get; set; }
    }

}
