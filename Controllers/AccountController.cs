using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace HelpDeskTI.Web.Controllers
{
    public class AccountController : Controller
    {
        // 1️⃣ Permite leer appsettings.json
        private readonly IConfiguration _configuration;

        // 2️⃣ Constructor: ASP.NET inyecta IConfiguration automáticamente
        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // =========================
        // GET: /Account/Login
        // =========================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // =========================
        // MÉTODO PARA GENERAR HASH
        // =========================
        private string ObtenerHash(string texto)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(
                    System.Text.Encoding.UTF8.GetBytes(texto)
                );

                return Convert.ToHexString(bytes).ToLower();
            }
        }

        // =========================
        // POST: /Account/Login
        // =========================
        [HttpPost]
        public IActionResult Login(string cedula, string clave)
        {
            // 3️⃣ Leemos la cadena de conexión desde appsettings.json
            string connectionString =
                _configuration.GetConnectionString("SoporteTI");

            // 4️⃣ Convertimos la contraseña digitada a HASH
            string hashIngresado = ObtenerHash(clave);

            // 5️⃣ Variable para guardar el rol
            string rol = "";

            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                // 6️⃣ OJO: Traemos Rol y ClaveHash (antes solo traías Rol)
                string sql = @"
                    SELECT Nombre, Apellido1, Rol, ClaveHash
                    FROM Usuarios
                    WHERE Cedula = @Cedula";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Cedula", cedula);

                cn.Open();

                SqlDataReader reader = cmd.ExecuteReader();

                // 7️⃣ Si existe la cédula
                if (reader.Read())
                {
                    rol = reader["Rol"].ToString();
                    string hashBD = reader["ClaveHash"].ToString();
                    string nombre = reader["Nombre"].ToString();
                    string apellido = reader["Apellido1"].ToString();
                    string nombreCompleto = nombre + " " + apellido;


                    // 8️⃣ Comparamos hashes
                    if (hashIngresado == hashBD)
                    {
                        // 9️⃣ Guardamos datos en sesión
                        HttpContext.Session.SetString("Usuario", nombreCompleto);
                        HttpContext.Session.SetString("Rol", rol);


                        // 🔟 Redirigimos (por ahora todos a Home)
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        ViewBag.Error = "Contraseña incorrecta.";
                        return View();
                    }
                }
                else
                {
                    ViewBag.Error = "La cédula no existe.";
                    return View();
                }
            }
        }

        // =========================
        // LOGOUT
        // =========================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}
