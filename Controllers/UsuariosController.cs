using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using HelpDeskTI.Web.Models;

namespace HelpDeskTI.Web.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly IConfiguration _configuration;

        // Constructor → permite leer appsettings.json
        public UsuariosController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // =========================================
        // LISTADO DE USUARIOS
        // GET: /Usuarios
        // =========================================
        public IActionResult Index()
        {
            List<Usuario> usuarios = new List<Usuario>();

            string connectionString = _configuration.GetConnectionString("SoporteTI");

            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                string sql = @"
                    SELECT Cedula, Nombre, Apellido1, Apellido2, Rol
                    FROM Usuarios";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    usuarios.Add(new Usuario
                    {
                        Cedula = reader["Cedula"].ToString(),
                        Nombre = reader["Nombre"].ToString(),
                        Apellido1 = reader["Apellido1"].ToString(),
                        Apellido2 = reader["Apellido2"].ToString(),
                        Rol = reader["Rol"].ToString()
                    });
                }
            }

            return View(usuarios);
        }

        // =========================================
        // NUEVO USUARIO (SOLO VISTA)
        // GET: /Usuarios/Nuevo
        // =========================================
        public IActionResult Nuevo()
        {
            return View();
        }

        public IActionResult Editar(string id)
        {
            ViewBag.Id = id;
            return View();
        }

        public IActionResult Eliminar(string id)
        {
            return View();
        }



        // =========================================
        // DETALLE DE USUARIO (SOLO VISTA)
        // GET: /Usuarios/Detalle
        // =========================================
        public IActionResult Detalle()
        {
            return View();
        }
    }
}
