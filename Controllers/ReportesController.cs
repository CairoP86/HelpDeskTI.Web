using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HelpDeskTI.Web.Controllers
{
    // 🔒 Solo Admin puede ver reportes
    [Authorize(Roles = "Admin")]
    public class ReportesController : Controller
    {
        private readonly IConfiguration _configuration;

        public ReportesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ======================================================
        // DASHBOARD GENERAL
        // ======================================================
        public IActionResult Index()
        {
            // ---------------- TIQUETES ----------------
            ViewBag.TiquetesPorEstado = EjecutarConsulta(@"
                SELECT Estado, COUNT(*) AS Total
                FROM Tiquetes
                GROUP BY Estado");

            ViewBag.TiquetesPorPrioridad = EjecutarConsulta(@"
                SELECT Prioridad, COUNT(*) AS Total
                FROM Tiquetes
                GROUP BY Prioridad");

            ViewBag.TiquetesPorMes = EjecutarConsulta(@"
                SELECT 
                    FORMAT(FechaCreacion, 'yyyy-MM') AS Mes,
                    COUNT(*) AS Total
                FROM Tiquetes
                GROUP BY FORMAT(FechaCreacion, 'yyyy-MM')
                ORDER BY Mes");

            ViewBag.PendientesPorTecnico = EjecutarConsulta(@"
                SELECT 
                    u.Nombre + ' ' + u.Apellido1 AS Tecnico,
                    COUNT(*) AS Pendientes
                FROM Tiquetes t
                INNER JOIN Usuarios u ON u.Cedula = t.CedulaTecnico
                WHERE t.Estado <> 'Cerrado'
                GROUP BY u.Nombre, u.Apellido1");

            ViewBag.TiempoPromedioResolucion = ObtenerTiempoPromedio();

            // ---------------- USUARIOS ----------------
            ViewBag.UsuariosUltimoMes = EjecutarConsulta(@"
                SELECT COUNT(*) AS Total
                FROM Usuarios
                WHERE FechaCreacion >= DATEADD(MONTH, -1, GETDATE())");

            ViewBag.UltimosUsuarios = EjecutarConsulta(@"
                SELECT TOP 5
                    Nombre,
                    Apellido1,
                    FechaCreacion
                FROM Usuarios
                ORDER BY FechaCreacion DESC");

            ViewBag.UsuariosInactivos = EjecutarConsulta(@"
                SELECT Nombre, Apellido1, Rol
                FROM Usuarios
                WHERE Activo = 0");

            // ---------------- COMPUTADORAS ----------------
            ViewBag.ComputadorasSinAsignar = EjecutarConsulta(@"
                SELECT CodigoActivo, Marca, Modelo
                FROM Computadoras
                WHERE CedulaUsuario IS NULL");

            // ---------------- SOFTWARE / LICENCIAS ----------------
            ViewBag.SoftwareAgotado = EjecutarConsulta(@"
                SELECT 
                    Nombre,
                    Version,
                    StockLicencias,
                    LicenciasInstaladas
                FROM Software
                WHERE LicenciasInstaladas >= StockLicencias");

            ViewBag.SoftwarePorAgotarse = EjecutarConsulta(@"
                SELECT 
                    Nombre,
                    Version,
                    (StockLicencias - LicenciasInstaladas) AS Disponibles
                FROM Software
                WHERE (StockLicencias - LicenciasInstaladas) BETWEEN 1 AND 3");

            ViewBag.SoftwareSinUso = EjecutarConsulta(@"
                SELECT Nombre, Version
                FROM Software
                WHERE LicenciasInstaladas = 0");

            return View();
        }

        // ======================================================
        // TIEMPO PROMEDIO DE RESOLUCIÓN (HORAS)
        // ======================================================
        private double ObtenerTiempoPromedio()
        {
            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"
                SELECT 
                    AVG(DATEDIFF(MINUTE, FechaCreacion, FechaCierre)) / 60.0
                FROM Tiquetes
                WHERE FechaCierre IS NOT NULL";

            using SqlCommand cmd = new(sql, cn);
            object? result = cmd.ExecuteScalar();

            return result == DBNull.Value || result == null
                ? 0
                : Convert.ToDouble(result);
        }

        // ======================================================
        // HELPER GENÉRICO
        // ======================================================
        private DataTable EjecutarConsulta(string sql)
        {
            DataTable table = new();

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            using SqlCommand cmd = new(sql, cn);
            using SqlDataReader dr = cmd.ExecuteReader();
            table.Load(dr);

            return table;
        }
    }
}
