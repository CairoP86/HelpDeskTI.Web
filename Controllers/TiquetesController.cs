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
        // ======================================================
        public IActionResult Index()
        {
            List<Tiquete> tiquetes = new();

            string rol = User.FindFirst(ClaimTypes.Role)?.Value!;
            string cedula = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = rol == "Usuario"
                ? @"SELECT * FROM Tiquetes WHERE CedulaCreador = @Cedula"
                : @"SELECT * FROM Tiquetes";

            using SqlCommand cmd = new(sql, cn);

            if (rol == "Usuario")
                cmd.Parameters.AddWithValue("@Cedula", cedula);

            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                tiquetes.Add(new Tiquete
                {
                    IdTiquete = (int)dr["IdTiquete"],
                    Titulo = dr["Titulo"].ToString()!,
                    Descripcion = dr["Descripcion"].ToString()!,
                    Estado = dr["Estado"].ToString()!,
                    Prioridad = dr["Prioridad"].ToString()!,
                    FechaCreacion = (DateTime)dr["FechaCreacion"],
                    FechaCierre = dr["FechaCierre"] == DBNull.Value ? null : (DateTime?)dr["FechaCierre"],
                    CedulaCreador = dr["CedulaCreador"].ToString()!,
                    CedulaTecnico = dr["CedulaTecnico"]?.ToString()
                });
            }

            return View(tiquetes);
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
        public IActionResult Nuevo(Tiquete t)
        {
            if (!ModelState.IsValid)
                return View(t);

            string cedula = User.Identity!.Name!;

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"INSERT INTO Tiquetes
                   (Titulo, Descripcion, Estado, Prioridad,
                    FechaCreacion, CedulaCreador)
                   VALUES
                   (@Titulo, @Descripcion, 'Abierto', @Prioridad,
                    GETDATE(), @CedulaCreador)";

            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@Titulo", t.Titulo);
            cmd.Parameters.AddWithValue("@Descripcion", t.Descripcion);
            cmd.Parameters.AddWithValue("@Prioridad", t.Prioridad);
            cmd.Parameters.AddWithValue("@CedulaCreador", cedula);

            cmd.ExecuteNonQuery();

            return RedirectToAction(nameof(Index));
        }

    }
}
