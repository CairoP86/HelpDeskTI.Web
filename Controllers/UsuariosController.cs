using HelpDeskTI.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace HelpDeskTI.Web.Controllers
{
    // 🔒 Solo Admin puede gestionar usuarios
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
        // Lista usuarios con departamento
        // ======================================================
        public IActionResult Index()
        {
            List<Usuario> usuarios = new();

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"
                SELECT 
                    u.Cedula,
                    u.Nombre,
                    u.Apellido1,
                    u.Rol,
                    u.Activo,
                    d.Nombre AS NombreDepartamento
                FROM Usuarios u
                LEFT JOIN Empleados e 
                    ON e.CedulaUsuario = u.Cedula
                LEFT JOIN Departamentos d 
                    ON d.IdDepartamento = e.IdDepartamento
                ORDER BY u.Nombre";

            using SqlCommand cmd = new(sql, cn);
            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                usuarios.Add(new Usuario
                {
                    Cedula = dr["Cedula"].ToString()!,
                    Nombre = $"{dr["Nombre"]} {dr["Apellido1"]}",
                    Rol = dr["Rol"].ToString()!,
                    Activo = Convert.ToBoolean(dr["Activo"]),
                    NombreDepartamento = dr["NombreDepartamento"]?.ToString()
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
            ViewBag.Departamentos = ObtenerDepartamentos();
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
                ViewBag.Departamentos = ObtenerDepartamentos();
                return View(u);
            }

            string hash = BCrypt.Net.BCrypt.HashPassword(clave);

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            using SqlTransaction tx = cn.BeginTransaction();

            try
            {
                // 1️⃣ Usuario
                string sqlUsuario = @"
                    INSERT INTO Usuarios
                    (Cedula, Nombre, Apellido1, Apellido2, Rol, ClaveHash, Activo, FechaCreacion, DebeCambiarClave)
                    VALUES
                    (@Cedula, @Nombre, @Apellido1, @Apellido2, @Rol, @ClaveHash, 1, GETDATE(), 1)";

                using SqlCommand cmdU = new(sqlUsuario, cn, tx);
                cmdU.Parameters.AddWithValue("@Cedula", u.Cedula);
                cmdU.Parameters.AddWithValue("@Nombre", u.Nombre);
                cmdU.Parameters.AddWithValue("@Apellido1", u.Apellido1);
                cmdU.Parameters.AddWithValue("@Apellido2", (object?)u.Apellido2 ?? DBNull.Value);
                cmdU.Parameters.AddWithValue("@Rol", u.Rol);
                cmdU.Parameters.AddWithValue("@ClaveHash", hash);
                cmdU.ExecuteNonQuery();

                // 2️⃣ Empleado (solo si no es Admin)
                if (u.Rol != "Admin" && u.IdDepartamento.HasValue)
                {
                    string sqlEmp = @"
                        INSERT INTO Empleados (CedulaUsuario, IdDepartamento)
                        VALUES (@Cedula, @IdDepartamento)";

                    using SqlCommand cmdE = new(sqlEmp, cn, tx);
                    cmdE.Parameters.AddWithValue("@Cedula", u.Cedula);
                    cmdE.Parameters.AddWithValue("@IdDepartamento", u.IdDepartamento.Value);
                    cmdE.ExecuteNonQuery();
                }

                tx.Commit();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                tx.Rollback();
                throw;
            }
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

            string sql = @"
                SELECT 
                    u.Cedula,
                    u.Nombre,
                    u.Apellido1,
                    u.Apellido2,
                    u.Rol,
                    u.Activo,
                    e.IdDepartamento
                FROM Usuarios u
                LEFT JOIN Empleados e 
                    ON e.CedulaUsuario = u.Cedula
                WHERE u.Cedula = @Cedula";

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
                    Activo = Convert.ToBoolean(dr["Activo"]),
                    IdDepartamento = dr["IdDepartamento"] as int?
                };
            }

            if (u == null)
                return NotFound();

            ViewBag.Departamentos = ObtenerDepartamentos();
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
            {
                ViewBag.Departamentos = ObtenerDepartamentos();
                return View(u);
            }

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            using SqlTransaction tx = cn.BeginTransaction();

            try
            {
                // Usuario
                string sqlU = @"
                    UPDATE Usuarios
                    SET
                        Nombre = @Nombre,
                        Apellido1 = @Apellido1,
                        Apellido2 = @Apellido2,
                        Rol = @Rol,
                        Activo = @Activo
                    WHERE Cedula = @Cedula";

                using SqlCommand cmdU = new(sqlU, cn, tx);
                cmdU.Parameters.AddWithValue("@Nombre", u.Nombre);
                cmdU.Parameters.AddWithValue("@Apellido1", u.Apellido1);
                cmdU.Parameters.AddWithValue("@Apellido2", (object?)u.Apellido2 ?? DBNull.Value);
                cmdU.Parameters.AddWithValue("@Rol", u.Rol);
                cmdU.Parameters.AddWithValue("@Activo", u.Activo);
                cmdU.Parameters.AddWithValue("@Cedula", u.Cedula);
                cmdU.ExecuteNonQuery();

                // Empleado (solo si no es Admin)
                if (u.Rol != "Admin")
                {
                    string sqlEmp = @"
                        IF EXISTS (SELECT 1 FROM Empleados WHERE CedulaUsuario = @Cedula)
                            UPDATE Empleados SET IdDepartamento = @IdDepartamento
                            WHERE CedulaUsuario = @Cedula
                        ELSE
                            INSERT INTO Empleados (CedulaUsuario, IdDepartamento)
                            VALUES (@Cedula, @IdDepartamento)";

                    using SqlCommand cmdE = new(sqlEmp, cn, tx);
                    cmdE.Parameters.AddWithValue("@Cedula", u.Cedula);
                    cmdE.Parameters.AddWithValue("@IdDepartamento", u.IdDepartamento ?? (object)DBNull.Value);
                    cmdE.ExecuteNonQuery();
                }

                tx.Commit();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // ======================================================
        // HELPER
        // ======================================================
        private List<Departamento> ObtenerDepartamentos()
        {
            List<Departamento> lista = new();

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = "SELECT IdDepartamento, Nombre FROM Departamentos ORDER BY Nombre";

            using SqlCommand cmd = new(sql, cn);
            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new Departamento
                {
                    IdDepartamento = (int)dr["IdDepartamento"],
                    Nombre = dr["Nombre"].ToString()!
                });
            }

            return lista;
        }
    }
}
