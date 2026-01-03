using System.ComponentModel.DataAnnotations;

namespace HelpDeskTI.Web.ViewModels.Usuarios
{
    public class UsuarioEditVM
    {
        [Required]
        public string Cedula { get; set; }

        [Required]
        public string Nombre { get; set; }

        [Required]
        public string Apellido1 { get; set; }

        public string? Apellido2 { get; set; }

        [Required]
        public string Rol { get; set; }

        public bool Activo { get; set; }

        [DataType(DataType.Password)]
        public string? NuevaClave { get; set; }
    }
}
