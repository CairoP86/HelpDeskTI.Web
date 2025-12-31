using Microsoft.AspNetCore.Mvc;

namespace HelpDeskTI.Web.Controllers
{
    public class ComputadorasController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Nuevo()
        {
            return View();
        }

        public IActionResult Detalle(int id)
        {
            ViewBag.Id = id;
            return View();
        }
    }
}
