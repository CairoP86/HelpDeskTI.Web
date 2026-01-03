using HelpDeskTI.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace HelpDeskTI.Web.Controllers
{
    [Authorize]
    public class SoftwareController : Controller
    {
        private readonly IConfiguration _configuration;

        public SoftwareController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ======================================================
        // INDEX
        // ======================================================
        public IActionResult Index()
        {
            List<Software> softwareList = new();

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"SELECT 
                                IdSoftware,
                                Nombre,
                                Version,
                                TipoLicencia,
                                StockLicencias,
                                LicenciasInstaladas
                           FROM Software
                           ORDER BY Nombre";

            using SqlCommand cmd = new(sql, cn);
            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                softwareList.Add(new Software
                {
                    IdSoftware = (int)dr["IdSoftware"],
                    Nombre = dr["Nombre"].ToString()!,
                    Version = dr["Version"].ToString()!,
                    TipoLicencia = dr["TipoLicencia"].ToString()!,
                    StockLicencias = Convert.ToInt32(dr["StockLicencias"]),
                    LicenciasInstaladas = Convert.ToInt32(dr["LicenciasInstaladas"])
                });
            }

            // 🔑 La vista usa @model → nunca View() vacío
            return View(softwareList);
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
        public IActionResult Nuevo(Software s)
        {
            if (!ModelState.IsValid)
                return View(s);

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"INSERT INTO Software
                           (Nombre, Version, TipoLicencia, StockLicencias, LicenciasInstaladas)
                           VALUES
                           (@Nombre, @Version, @TipoLicencia, @StockLicencias, @LicenciasInstaladas)";

            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@Nombre", s.Nombre);
            cmd.Parameters.AddWithValue("@Version", s.Version);
            cmd.Parameters.AddWithValue("@TipoLicencia", s.TipoLicencia);
            cmd.Parameters.AddWithValue("@StockLicencias", s.StockLicencias);
            cmd.Parameters.AddWithValue("@LicenciasInstaladas", s.LicenciasInstaladas);

            cmd.ExecuteNonQuery();

            return RedirectToAction(nameof(Index));
        }
    }
}
