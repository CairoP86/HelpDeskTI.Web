using HelpDeskTI.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace HelpDeskTI.Web.Controllers
{
    [Authorize]
    public class TiquetesController : Controller
    {
        private readonly IConfiguration _configuration;

        public TiquetesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ======================================================
        // INDEX
        // Lista de tiquetes según rol
        // ======================================================
        public IActionResult Index()
        {
            List<Tiquete> lista = new();

            string cedula = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            string rol = User.FindFirstValue(ClaimTypes.Role)!;

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"
                SELECT 
                    t.IdTiquete,
                    t.Titulo,
                    t.Estado,
                    t.Prioridad,
                    t.FechaCreacion,
                    u.Nombre + ' ' + u.Apellido1 AS Creador,
                    tec.Nombre + ' ' + tec.Apellido1 AS Tecnico
                FROM Tiquetes t
                INNER JOIN Usuarios u ON u.Cedula = t.CedulaCreador
                LEFT JOIN Usuarios tec ON tec.Cedula = t.CedulaTecnico
            ";

            if (rol == "Usuario")
                sql += " WHERE t.CedulaCreador = @Cedula";

            if (rol == "Tecnico")
                sql += " WHERE t.CedulaTecnico = @Cedula";

            sql += " ORDER BY t.FechaCreacion DESC";

            using SqlCommand cmd = new(sql, cn);
            if (rol != "Admin")
                cmd.Parameters.AddWithValue("@Cedula", cedula);

            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new Tiquete
                {
                    IdTiquete = (int)dr["IdTiquete"],
                    Titulo = dr["Titulo"].ToString()!,
                    Estado = dr["Estado"].ToString()!,
                    Prioridad = dr["Prioridad"]?.ToString(),
                    FechaCreacion = (DateTime)dr["FechaCreacion"],
                    NombreCreador = dr["Creador"].ToString(),
                    NombreTecnico = dr["Tecnico"]?.ToString()
                });
            }

            return View(lista);
        }

        // ======================================================
        // NUEVO (GET) — solo Usuario
        // ======================================================
        [Authorize(Roles = "Usuario")]
        public IActionResult Nuevo()
        {
            return View();
        }

        // ======================================================
        // NUEVO (POST)
        // ======================================================
        [HttpPost]
        [Authorize(Roles = "Usuario")]
        [ValidateAntiForgeryToken]
        public IActionResult Nuevo(Tiquete t)
        {
            if (!ModelState.IsValid)
                return View(t);

            string cedula = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"
                INSERT INTO Tiquetes
                (Titulo, Descripcion, Estado, Prioridad, CedulaCreador, FechaCreacion)
                VALUES
                (@Titulo, @Descripcion, 'Abierto', @Prioridad, @CedulaCreador, GETDATE())";

            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@Titulo", t.Titulo);
            cmd.Parameters.AddWithValue("@Descripcion", t.Descripcion);
            cmd.Parameters.AddWithValue("@Prioridad", t.Prioridad ?? "Media");
            cmd.Parameters.AddWithValue("@CedulaCreador", cedula);

            cmd.ExecuteNonQuery();

            return RedirectToAction(nameof(Index));
        }

        // ======================================================
        // DETALLE
        // ======================================================
        public IActionResult Detalle(int id)
        {
            Tiquete? t = null;

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"
                SELECT 
                    t.IdTiquete,
                    t.Titulo,
                    t.Descripcion,
                    t.Estado,
                    t.Prioridad,
                    t.FechaCreacion,
                    u.Nombre + ' ' + u.Apellido1 AS Creador,
                    tec.Nombre + ' ' + tec.Apellido1 AS Tecnico,
                    t.CedulaTecnico
                FROM Tiquetes t
                INNER JOIN Usuarios u ON u.Cedula = t.CedulaCreador
                LEFT JOIN Usuarios tec ON tec.Cedula = t.CedulaTecnico
                WHERE t.IdTiquete = @Id";

            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@Id", id);

            using SqlDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                t = new Tiquete
                {
                    IdTiquete = (int)dr["IdTiquete"],
                    Titulo = dr["Titulo"].ToString()!,
                    Descripcion = dr["Descripcion"].ToString(),
                    Estado = dr["Estado"].ToString()!,
                    Prioridad = dr["Prioridad"]?.ToString(),
                    FechaCreacion = (DateTime)dr["FechaCreacion"],
                    NombreCreador = dr["Creador"].ToString(),
                    NombreTecnico = dr["Tecnico"]?.ToString(),
                    CedulaTecnico = dr["CedulaTecnico"]?.ToString()
                };
            }

            if (t == null)
                return NotFound();

            ViewBag.Tecnicos = ObtenerTecnicos();
            return View(t);
        }

        // ======================================================
        // ASIGNAR TECNICO — Admin
        // ======================================================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult AsignarTecnico(int idTiquete, string cedulaTecnico)
        {
            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"
                UPDATE Tiquetes
                SET CedulaTecnico = @Tecnico,
                    Estado = 'En Proceso'
                WHERE IdTiquete = @Id";

            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@Tecnico", cedulaTecnico);
            cmd.Parameters.AddWithValue("@Id", idTiquete);

            cmd.ExecuteNonQuery();

            return RedirectToAction("Detalle", new { id = idTiquete });
        }

        // ======================================================
        // CAMBIAR ESTADO — Tecnico / Admin
        // ======================================================
        [HttpPost]
        [Authorize(Roles = "Tecnico,Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult CambiarEstado(int idTiquete, string estado)
        {
            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"
                UPDATE Tiquetes
                SET Estado = @Estado
                WHERE IdTiquete = @Id";

            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@Estado", estado);
            cmd.Parameters.AddWithValue("@Id", idTiquete);

            cmd.ExecuteNonQuery();

            return RedirectToAction("Detalle", new { id = idTiquete });
        }

        // ======================================================
        // HELPERS
        // ======================================================
        private List<Usuario> ObtenerTecnicos()
        {
            List<Usuario> lista = new();

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"
                SELECT Cedula, Nombre, Apellido1
                FROM Usuarios
                WHERE Rol = 'Tecnico' AND Activo = 1
                ORDER BY Nombre";

            using SqlCommand cmd = new(sql, cn);
            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new Usuario
                {
                    Cedula = dr["Cedula"].ToString()!,
                    Nombre = dr["Nombre"] + " " + dr["Apellido1"]
                });
            }

            return lista;
        }
    }
}
