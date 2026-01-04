using HelpDeskTI.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace HelpDeskTI.Web.Controllers
{
    [Authorize]
    public class ComputadorasController : Controller
    {
        private readonly IConfiguration _configuration;

        public ComputadorasController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ======================================================
        // INDEX
        // Lista computadoras + usuario asignado (si existe)
        // ======================================================
        public IActionResult Index()
        {
            List<Computadora> computadoras = new();

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            // ✔ JOIN correcto contra Usuarios
            string sql = @"
                SELECT 
                    c.IdComputadora,
                    c.CodigoActivo,
                    c.Marca,
                    c.Modelo,
                    c.Serie,
                    u.Nombre,
                    u.Apellido1
                FROM Computadoras c
                LEFT JOIN Usuarios u 
                    ON c.CedulaUsuario = u.Cedula
                ORDER BY c.CodigoActivo";

            using SqlCommand cmd = new(sql, cn);
            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                computadoras.Add(new Computadora
                {
                    IdComputadora = (int)dr["IdComputadora"],
                    CodigoActivo = dr["CodigoActivo"].ToString()!,
                    Marca = dr["Marca"]?.ToString(),
                    Modelo = dr["Modelo"]?.ToString(),
                    Serie = dr["Serie"]?.ToString(),

                    // Solo para vistas
                    NombreUsuarioAsignado =
                        dr["Nombre"] == DBNull.Value
                            ? "Sin asignar"
                            : $"{dr["Nombre"]} {dr["Apellido1"]}"
                });
            }

            return View(computadoras);
        }

        // ======================================================
        // DETALLE
        // Muestra computadora + usuario asignado
        // ======================================================
        [HttpGet]
        public IActionResult Detalle(int id)
        {
            Computadora? c = null;

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"
        SELECT 
            c.IdComputadora,
            c.CodigoActivo,
            c.Marca,
            c.Modelo,
            c.Serie,
            c.CedulaUsuario,
            u.Nombre,
            u.Apellido1
        FROM Computadoras c
        LEFT JOIN Usuarios u 
            ON c.CedulaUsuario = u.Cedula
        WHERE c.IdComputadora = @Id";

            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@Id", id);

            using SqlDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                c = new Computadora
                {
                    IdComputadora = (int)dr["IdComputadora"],
                    CodigoActivo = dr["CodigoActivo"].ToString()!,
                    Marca = dr["Marca"]?.ToString(),
                    Modelo = dr["Modelo"]?.ToString(),
                    Serie = dr["Serie"]?.ToString(),
                    CedulaUsuario = dr["CedulaUsuario"] == DBNull.Value
                        ? null
                        : dr["CedulaUsuario"].ToString(),
                    NombreUsuarioAsignado =
                        dr["Nombre"] == DBNull.Value
                            ? null
                            : $"{dr["Nombre"]} {dr["Apellido1"]}"
                };
            }

            if (c == null)
                return NotFound();

            return View(c);
        }


        // ======================================================
        // NUEVO (GET)
        // ======================================================
        [Authorize(Roles = "Admin,Tecnico")]
        [HttpGet]
        public IActionResult Nuevo()
        {
            return View();
        }

        // ======================================================
        // NUEVO (POST)
        // ======================================================
        [Authorize(Roles = "Admin,Tecnico")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Nuevo(Computadora c)
        {
            if (!ModelState.IsValid)
                return View(c);

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"
                INSERT INTO Computadoras
                (CodigoActivo, Marca, Modelo, Serie)
                VALUES
                (@CodigoActivo, @Marca, @Modelo, @Serie)";

            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@CodigoActivo", c.CodigoActivo);
            cmd.Parameters.AddWithValue("@Marca", c.Marca);
            cmd.Parameters.AddWithValue("@Modelo", c.Modelo);
            cmd.Parameters.AddWithValue("@Serie",
                string.IsNullOrWhiteSpace(c.Serie)
                    ? (object)DBNull.Value
                    : c.Serie);

            cmd.ExecuteNonQuery();

            return RedirectToAction(nameof(Index));
        }

        // ======================================================
        // ASIGNAR (GET)
        // Selecciona usuario para la computadora
        // ======================================================
        [Authorize(Roles = "Admin,Tecnico")]
        [HttpGet]
        public IActionResult Asignar(int id)
        {
            Computadora? computadora = null;
            List<Usuario> usuarios = new();

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            // Se obtiene CedulaUsuario para validar si ya está asignada
            string sqlComp = @"
                SELECT IdComputadora, CodigoActivo, CedulaUsuario
                FROM Computadoras
                WHERE IdComputadora = @Id";

            using (SqlCommand cmd = new(sqlComp, cn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                using SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    computadora = new Computadora
                    {
                        IdComputadora = (int)dr["IdComputadora"],
                        CodigoActivo = dr["CodigoActivo"].ToString()!,
                        CedulaUsuario = dr["CedulaUsuario"]?.ToString()
                    };
                }
            }

            if (computadora == null)
                return NotFound();

            // 🔒 Regla de negocio: no reasignar si ya tiene usuario
            if (!string.IsNullOrEmpty(computadora.CedulaUsuario))
            {
                TempData["Error"] = "Esta computadora ya está asignada.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            // Lista de usuarios activos
            string sqlUsers = @"
                SELECT Cedula, Nombre, Apellido1
                FROM Usuarios
                WHERE Activo = 1
                ORDER BY Nombre";

            using (SqlCommand cmd = new(sqlUsers, cn))
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    usuarios.Add(new Usuario
                    {
                        Cedula = dr["Cedula"].ToString()!,
                        Nombre = $"{dr["Nombre"]} {dr["Apellido1"]}"
                    });
                }
            }

            ViewBag.Usuarios = usuarios;

            return View(computadora);
        }

        // ======================================================
        // ASIGNAR (POST)
        // ======================================================
        [Authorize(Roles = "Admin,Tecnico")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Asignar(int idComputadora, string cedulaUsuario)
        {
            string cs = _configuration.GetConnectionString("SoporteTI");

            try
            {
                using SqlConnection cn = new(cs);
                cn.Open();

                string sql = @"
            UPDATE Computadoras
            SET CedulaUsuario = @CedulaUsuario
            WHERE IdComputadora = @IdComputadora";

                using SqlCommand cmd = new(sql, cn);
                cmd.Parameters.AddWithValue("@CedulaUsuario", cedulaUsuario);
                cmd.Parameters.AddWithValue("@IdComputadora", idComputadora);

                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                // Violación de índice UNIQUE
                TempData["Error"] = "Este usuario ya tiene una computadora asignada.";
                return RedirectToAction(nameof(Asignar), new { id = idComputadora });
            }

            return RedirectToAction(nameof(Detalle), new { id = idComputadora });
        }


        // ======================================================
        // DESASIGNAR
        // ======================================================
        [Authorize(Roles = "Admin,Tecnico")]
        public IActionResult Desasignar(int id)
        {
            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"
                UPDATE Computadoras
                SET CedulaUsuario = NULL
                WHERE IdComputadora = @Id";

            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();

            return RedirectToAction(nameof(Detalle), new { id });
        }
    }
}
