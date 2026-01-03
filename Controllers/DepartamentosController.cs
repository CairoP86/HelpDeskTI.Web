using HelpDeskTI.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace HelpDeskTI.Web.Controllers
{
    [Authorize]
    public class DepartamentosController : Controller
    {
        private readonly IConfiguration _configuration;

        public DepartamentosController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ======================================================
        // INDEX
        // ======================================================
        public IActionResult Index()
        {
            List<Departamento> departamentos = new();

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"SELECT IdDepartamento, Nombre
                           FROM Departamentos
                           ORDER BY Nombre";

            using SqlCommand cmd = new(sql, cn);
            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                departamentos.Add(new Departamento
                {
                    IdDepartamento = (int)dr["IdDepartamento"],
                    Nombre = dr["Nombre"].ToString()!
                });
            }

            // 🔑 CLAVE: la vista usa @model → nunca View() vacío
            return View(departamentos);
        }

        // ======================================================
        // NUEVO (GET)
        // ======================================================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Nuevo()
        {
            return View();
        }

        // ======================================================
        // NUEVO (POST)
        // ======================================================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Nuevo(Departamento d)
        {
            if (!ModelState.IsValid)
                return View(d);

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"INSERT INTO Departamentos (Nombre)
                           VALUES (@Nombre)";

            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@Nombre", d.Nombre);

            cmd.ExecuteNonQuery();

            return RedirectToAction(nameof(Index));
        }

        // ======================================================
        // EDITAR (GET)
        // ======================================================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Editar(int id)
        {
            Departamento? d = null;

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"SELECT IdDepartamento, Nombre
                           FROM Departamentos
                           WHERE IdDepartamento = @Id";

            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@Id", id);

            using SqlDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                d = new Departamento
                {
                    IdDepartamento = (int)dr["IdDepartamento"],
                    Nombre = dr["Nombre"].ToString()!
                };
            }

            if (d == null)
                return NotFound();

            return View(d);
        }

        // ======================================================
        // EDITAR (POST)
        // ======================================================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Editar(Departamento d)
        {
            if (!ModelState.IsValid)
                return View(d);

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"UPDATE Departamentos
                           SET Nombre = @Nombre
                           WHERE IdDepartamento = @Id";

            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@Nombre", d.Nombre);
            cmd.Parameters.AddWithValue("@Id", d.IdDepartamento);

            cmd.ExecuteNonQuery();

            return RedirectToAction(nameof(Index));
        }
    }
}
