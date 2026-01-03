using HelpDeskTI.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace HelpDeskTI.Web.Controllers
{
    // 🔐 Solo usuarios autenticados pueden acceder
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
        // Lista de computadoras
        // ======================================================
        public IActionResult Index()
        {
            List<Computadora> computadoras = new();

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"SELECT 
                        IdComputadora,
                        CodigoActivo,
                        Marca,
                        Modelo,
                        Serie,
                        IdEmpleado
                   FROM Computadoras
                   ORDER BY CodigoActivo";

            using SqlCommand cmd = new(sql, cn);
            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                computadoras.Add(new Computadora
                {
                    IdComputadora = (int)dr["IdComputadora"],
                    CodigoActivo = dr["CodigoActivo"].ToString()!,
                    Marca = dr["Marca"].ToString()!,
                    Modelo = dr["Modelo"].ToString()!,
                    Serie = dr["Serie"]?.ToString(),
                    IdEmpleado = dr["IdEmpleado"] == DBNull.Value
                                    ? null
                                    : Convert.ToInt32(dr["IdEmpleado"])
                });
            }

            return View(computadoras);
        }


        // ======================================================
        // NUEVO (GET)
        // Solo Admin o Técnico pueden registrar computadoras
        // ======================================================
        [Authorize(Roles = "Admin,Tecnico")]
        [HttpGet]
        public IActionResult Nuevo()
        {
            return View();
        }

        // ======================================================
        // DETALLE
        // Muestra información detallada de una computadora
        // ======================================================
        [HttpGet]
        public IActionResult Detalle(int id)
        {
            // En esta versión solo validamos el id
            // Más adelante se puede cargar desde BD
            if (id <= 0)
                return NotFound();

            ViewBag.Id = id;
            return View();
        }
    }
}
