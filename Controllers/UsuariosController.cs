using HelpDeskTI.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace HelpDeskTI.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsuariosController : Controller
    {
        private readonly IConfiguration _configuration;

        public UsuariosController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ======================================================
        // INDEX
        // ======================================================
        public IActionResult Index()
        {
            List<Usuario> usuarios = new();

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"SELECT Cedula, Nombre, Apellido1, Apellido2, Rol, Activo
                           FROM Usuarios
                           ORDER BY Nombre";

            using SqlCommand cmd = new(sql, cn);
            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                usuarios.Add(new Usuario
                {
                    Cedula = dr["Cedula"].ToString()!,
                    Nombre = dr["Nombre"].ToString()!,
                    Apellido1 = dr["Apellido1"].ToString()!,
                    Apellido2 = dr["Apellido2"]?.ToString(),
                    Rol = dr["Rol"].ToString()!,
                    Activo = Convert.ToBoolean(dr["Activo"])
                });
            }

            return View(usuarios);
        }

        // ======================================================
        // NUEVO (GET)
        // ======================================================
        [HttpGet]
        public IActionResult Nuevo()
        {
            return View();
        }

        // ======================================================
        // NUEVO (POST)
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Nuevo(Usuario u, string clave)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(clave))
            {
                ModelState.AddModelError("", "La contraseña es obligatoria");
                return View(u);
            }

            string hash = BCrypt.Net.BCrypt.HashPassword(clave);

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"INSERT INTO Usuarios
                           (Cedula, Nombre, Apellido1, Apellido2, Rol,
                            ClaveHash, Activo, FechaCreacion,
                            DebeCambiarClave)
                           VALUES
                           (@Cedula, @Nombre, @Apellido1, @Apellido2, @Rol,
                            @ClaveHash, 1, GETDATE(), 1)";

            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@Cedula", u.Cedula);
            cmd.Parameters.AddWithValue("@Nombre", u.Nombre);
            cmd.Parameters.AddWithValue("@Apellido1", u.Apellido1);
            cmd.Parameters.AddWithValue("@Apellido2", u.Apellido2 ?? "");
            cmd.Parameters.AddWithValue("@Rol", u.Rol);
            cmd.Parameters.AddWithValue("@ClaveHash", hash);

            cmd.ExecuteNonQuery();

            return RedirectToAction(nameof(Index));
        }

        // ======================================================
        // EDITAR (GET)
        // ======================================================
        [HttpGet]
        public IActionResult Editar(string id)
        {
            Usuario? u = null;

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"SELECT Cedula, Nombre, Apellido1, Apellido2, Rol, Activo
                           FROM Usuarios
                           WHERE Cedula = @Cedula";

            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@Cedula", id);

            using SqlDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                u = new Usuario
                {
                    Cedula = dr["Cedula"].ToString()!,
                    Nombre = dr["Nombre"].ToString()!,
                    Apellido1 = dr["Apellido1"].ToString()!,
                    Apellido2 = dr["Apellido2"]?.ToString(),
                    Rol = dr["Rol"].ToString()!,
                    Activo = Convert.ToBoolean(dr["Activo"])
                };
            }

            if (u == null)
                return NotFound();

            return View(u);
        }

        // ======================================================
        // EDITAR (POST)
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Editar(Usuario u)
        {
            if (!ModelState.IsValid)
                return View(u);

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"UPDATE Usuarios SET
                           Nombre = @Nombre,
                           Apellido1 = @Apellido1,
                           Apellido2 = @Apellido2,
                           Rol = @Rol,
                           Activo = @Activo
                           WHERE Cedula = @Cedula";

            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@Nombre", u.Nombre);
            cmd.Parameters.AddWithValue("@Apellido1", u.Apellido1);
            cmd.Parameters.AddWithValue("@Apellido2", u.Apellido2 ?? "");
            cmd.Parameters.AddWithValue("@Rol", u.Rol);
            cmd.Parameters.AddWithValue("@Activo", u.Activo);
            cmd.Parameters.AddWithValue("@Cedula", u.Cedula);

            cmd.ExecuteNonQuery();

            return RedirectToAction(nameof(Index));
        }
    }
}
