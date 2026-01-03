namespace HelpDeskTI.Web.Models
{
    public class Notificacion
    {
        public int IdNotificacion { get; set; }
        public string CedulaUsuario { get; set; }
        public string Mensaje { get; set; }
        public bool Visto { get; set; }
        public DateTime Fecha { get; set; }
    }
}
