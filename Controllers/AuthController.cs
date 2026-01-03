using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace HelpDeskTI.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ======================================================
        // LOGIN (GET)
        // Muestra la pantalla de login
        // Si ya está autenticado, redirige al Home
        // ======================================================
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View();
        }

        // ======================================================
        // LOGIN (POST)
        // Valida credenciales y crea cookie de autenticación
        // ======================================================
        [HttpPost]
        public async Task<IActionResult> Login(string cedula, string clave)
        {
            // Validación básica
            if (string.IsNullOrWhiteSpace(cedula) || string.IsNullOrWhiteSpace(clave))
            {
                ViewBag.Error = "Debe ingresar usuario y contraseña";
                return View();
            }

            string cs = _configuration.GetConnectionString("SoporteTI");

            using SqlConnection cn = new(cs);
            cn.Open();

            string sql = @"
                SELECT Cedula, Nombre, Rol, ClaveHash, Activo
                FROM Usuarios
                WHERE Cedula = @Cedula
            ";

            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@Cedula", cedula);

            using SqlDataReader dr = cmd.ExecuteReader();

            // Usuario no existe o está inactivo
            if (!dr.Read() || !(bool)dr["Activo"])
            {
                ViewBag.Error = "Credenciales inválidas";
                return View();
            }

            string claveHash = dr["ClaveHash"].ToString();

            // Verificar contraseña con BCrypt
            if (!BCrypt.Net.BCrypt.Verify(clave, claveHash))
            {
                ViewBag.Error = "Credenciales inválidas";
                return View();
            }

            // ==================================================
            // CREACIÓN DE CLAIMS (BASE DE SEGURIDAD)
            // ==================================================
            var claims = new List<Claim>
            {
                // Identidad principal (cédula / usuario)
                new Claim(ClaimTypes.Name, dr["Cedula"].ToString()),

                // Rol (Admin, Tecnico, Empleado)
                new Claim(ClaimTypes.Role, dr["Rol"].ToString()),

                // Nombre visible en la UI
                new Claim("Nombre", dr["Nombre"].ToString())
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            // ==================================================
            // LOGIN CON COOKIE
            // ==================================================
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal
            );

            return RedirectToAction("Index", "Home");
        }

        // ======================================================
        // LOGOUT
        // Elimina la cookie de autenticación
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction("Login");
        }
    }
}
