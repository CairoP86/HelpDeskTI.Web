using System.Diagnostics;
using HelpDeskTI.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace HelpDeskTI.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Usuario") == null)
            {
                return RedirectToAction("Login", "Account");
            }
            ViewBag.Usuario = HttpContext.Session.GetString("Usuario");
            ViewBag.Rol = HttpContext.Session.GetString("Rol");

            ViewBag.Usuario = HttpContext.Session.GetString("Usuario");

            return View();
        }




    }
}
