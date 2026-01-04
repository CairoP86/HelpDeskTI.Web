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

        // ================================
        // INDEX
        // ================================
        public IActionResult Index()
        {
            List<Software> lista = new();

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"SELECT IdSoftware, Nombre, Version, TipoLicencia,
                                  StockLicencias, LicenciasInstaladas
                           FROM Software
                           ORDER BY Nombre";

            using SqlCommand cmd = new(sql, cn);
            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new Software
                {
                    IdSoftware = (int)dr["IdSoftware"],
                    Nombre = dr["Nombre"].ToString()!,
                    Version = dr["Version"]?.ToString(),
                    TipoLicencia = dr["TipoLicencia"].ToString()!,
                    StockLicencias = (int)dr["StockLicencias"],
                    LicenciasInstaladas = (int)dr["LicenciasInstaladas"]
                });
            }

            return View(lista);
        }

        // ================================
        // NUEVO (GET)
        // ================================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Nuevo()
        {
            return View();
        }

        // ================================
        // NUEVO (POST)
        // ================================
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
                           (@Nombre, @Version, @TipoLicencia, @Stock, 0)";

            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@Nombre", s.Nombre);
            cmd.Parameters.AddWithValue("@Version", s.Version ?? "");
            cmd.Parameters.AddWithValue("@TipoLicencia", s.TipoLicencia);
            cmd.Parameters.AddWithValue("@Stock", s.StockLicencias);

            cmd.ExecuteNonQuery();
            return RedirectToAction(nameof(Index));
        }

        // ======================================================
        // ASIGNAR SOFTWARE (GET)
        // ======================================================
        [HttpGet]
        public IActionResult Asignar(int id)
        {
            ViewBag.IdSoftware = id;

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"SELECT IdComputadora, CodigoActivo
                           FROM Computadoras
                           ORDER BY CodigoActivo";

            List<dynamic> computadoras = new();

            using SqlCommand cmd = new(sql, cn);
            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                computadoras.Add(new
                {
                    IdComputadora = (int)dr["IdComputadora"],
                    CodigoActivo = dr["CodigoActivo"].ToString()
                });
            }

            ViewBag.Computadoras = computadoras;
            return View();
        }

        // ======================================================
        // ASIGNAR SOFTWARE (POST)
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Asignar(int idSoftware, int idComputadora)
        {
            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            using SqlTransaction tx = cn.BeginTransaction();

            try
            {
                // 1️⃣ Verificar licencias
                string checkSql = @"SELECT StockLicencias, LicenciasInstaladas
                                    FROM Software
                                    WHERE IdSoftware = @Id";

                using SqlCommand checkCmd = new(checkSql, cn, tx);
                checkCmd.Parameters.AddWithValue("@Id", idSoftware);

                using SqlDataReader dr = checkCmd.ExecuteReader();
                if (!dr.Read())
                {
                    dr.Close();
                    tx.Rollback();
                    return NotFound();
                }

                int stock = (int)dr["StockLicencias"];
                int usadas = (int)dr["LicenciasInstaladas"];
                dr.Close();

                if (usadas >= stock)
                {
                    tx.Rollback();
                    TempData["Error"] = "No hay licencias disponibles.";
                    return RedirectToAction("Index");
                }

                // 2️⃣ Validar duplicado
                string existeSql = @"SELECT COUNT(*)
                                     FROM SoftwareComputadora
                                     WHERE IdSoftware = @IdSoftware
                                       AND IdComputadora = @IdComputadora";

                using SqlCommand existeCmd = new(existeSql, cn, tx);
                existeCmd.Parameters.AddWithValue("@IdSoftware", idSoftware);
                existeCmd.Parameters.AddWithValue("@IdComputadora", idComputadora);

                if ((int)existeCmd.ExecuteScalar() > 0)
                {
                    tx.Rollback();
                    TempData["Error"] = "Este software ya está asignado a esa computadora.";
                    return RedirectToAction("Index");
                }

                // 3️⃣ Insertar relación
                string insertSql = @"INSERT INTO SoftwareComputadora
                                     (IdSoftware, IdComputadora)
                                     VALUES (@IdSoftware, @IdComputadora)";

                using SqlCommand insertCmd = new(insertSql, cn, tx);
                insertCmd.Parameters.AddWithValue("@IdSoftware", idSoftware);
                insertCmd.Parameters.AddWithValue("@IdComputadora", idComputadora);
                insertCmd.ExecuteNonQuery();

                // 3️⃣.1 Historial
                string historialSql = @"INSERT INTO SoftwareHistorial
                                        (IdSoftware, IdComputadora, Accion)
                                        VALUES (@IdSoftware, @IdComputadora, 'ASIGNADO')";

                using SqlCommand histCmd = new(historialSql, cn, tx);
                histCmd.Parameters.AddWithValue("@IdSoftware", idSoftware);
                histCmd.Parameters.AddWithValue("@IdComputadora", idComputadora);
                histCmd.ExecuteNonQuery();

                // 4️⃣ Incrementar licencia
                string updateSql = @"UPDATE Software
                                     SET LicenciasInstaladas = LicenciasInstaladas + 1
                                     WHERE IdSoftware = @Id";

                using SqlCommand updateCmd = new(updateSql, cn, tx);
                updateCmd.Parameters.AddWithValue("@Id", idSoftware);
                updateCmd.ExecuteNonQuery();

                tx.Commit();
                return RedirectToAction("Index");
            }
            catch
            {
                tx.Rollback();
                TempData["Error"] = "Ocurrió un error al asignar el software.";
                return RedirectToAction("Index");
            }
        }

        // ======================================================
        // QUITAR SOFTWARE (POST)
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Quitar(int idSoftware, int idComputadora)
        {
            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            using SqlTransaction tx = cn.BeginTransaction();

            try
            {
                // 1️⃣ Eliminar relación
                string deleteSql = @"DELETE FROM SoftwareComputadora
                                     WHERE IdSoftware = @IdSoftware
                                       AND IdComputadora = @IdComputadora";

                using SqlCommand deleteCmd = new(deleteSql, cn, tx);
                deleteCmd.Parameters.AddWithValue("@IdSoftware", idSoftware);
                deleteCmd.Parameters.AddWithValue("@IdComputadora", idComputadora);

                if (deleteCmd.ExecuteNonQuery() == 0)
                {
                    tx.Rollback();
                    TempData["Error"] = "La asignación no existe.";
                    return RedirectToAction("Index");
                }

                // 1️⃣.1 Historial
                string historialSql = @"INSERT INTO SoftwareHistorial
                                        (IdSoftware, IdComputadora, Accion)
                                        VALUES (@IdSoftware, @IdComputadora, 'QUITADO')";

                using SqlCommand histCmd = new(historialSql, cn, tx);
                histCmd.Parameters.AddWithValue("@IdSoftware", idSoftware);
                histCmd.Parameters.AddWithValue("@IdComputadora", idComputadora);
                histCmd.ExecuteNonQuery();

                // 2️⃣ Decrementar licencia
                string updateSql = @"UPDATE Software
                                     SET LicenciasInstaladas = LicenciasInstaladas - 1
                                     WHERE IdSoftware = @IdSoftware
                                       AND LicenciasInstaladas > 0";

                using SqlCommand updateCmd = new(updateSql, cn, tx);
                updateCmd.Parameters.AddWithValue("@IdSoftware", idSoftware);

                if (updateCmd.ExecuteNonQuery() == 0)
                {
                    tx.Rollback();
                    TempData["Error"] = "No se pudo actualizar la licencia.";
                    return RedirectToAction("Index");
                }

                tx.Commit();
                return RedirectToAction("Index");
            }
            catch
            {
                tx.Rollback();
                TempData["Error"] = "No se pudo quitar el software.";
                return RedirectToAction("Index");
            }
        }

        // ======================================================
        // SOFTWARE POR COMPUTADORA (GET)
        // ======================================================
        [HttpGet]
        public IActionResult SoftwarePorComputadora(int id)
        {
            ViewBag.IdComputadora = id;

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            // 🔹 Obtener código del equipo
            string sqlEquipo = @"SELECT CodigoActivo
                         FROM Computadoras
                         WHERE IdComputadora = @Id";

            using SqlCommand cmdEquipo = new(sqlEquipo, cn);
            cmdEquipo.Parameters.AddWithValue("@Id", id);

            ViewBag.CodigoActivo = cmdEquipo.ExecuteScalar()?.ToString();

            // 🔹 Software instalado
            List<Software> lista = new();

            string sql = @"SELECT s.IdSoftware, s.Nombre, s.Version
                   FROM SoftwareComputadora sc
                   INNER JOIN Software s ON sc.IdSoftware = s.IdSoftware
                   WHERE sc.IdComputadora = @Id
                   ORDER BY s.Nombre";

            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@Id", id);

            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new Software
                {
                    IdSoftware = (int)dr["IdSoftware"],
                    Nombre = dr["Nombre"].ToString()!,
                    Version = dr["Version"]?.ToString()
                });
            }

            return View(lista);
        }


        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Historial()
        {
            List<dynamic> lista = new();

            string cs = _configuration.GetConnectionString("SoporteTI");
            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"SELECT 
                        s.Nombre AS Software,
                        c.CodigoActivo,
                        h.Accion,
                        h.Fecha
                   FROM SoftwareHistorial h
                   INNER JOIN Software s ON h.IdSoftware = s.IdSoftware
                   INNER JOIN Computadoras c ON h.IdComputadora = c.IdComputadora
                   ORDER BY h.Fecha DESC";

            using SqlCommand cmd = new(sql, cn);
            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new
                {
                    Software = dr["Software"].ToString(),
                    CodigoActivo = dr["CodigoActivo"].ToString(),
                    Accion = dr["Accion"].ToString(),
                    Fecha = (DateTime)dr["Fecha"]
                });
            }

            return View(lista);
        }

    }
}
