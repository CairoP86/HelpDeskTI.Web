using Microsoft.AspNetCore.Mvc;

namespace HelpDeskTI.Web.Controllers
{
    public class TiquetesController : Controller
    {
        // LISTA
        public IActionResult Index()
        {
            return View();
        }

        // FORM NUEVO
        public IActionResult Nuevo()
        {
            return View();
        }

        // DETALLE
        public IActionResult Detalle(int id)
        {
            ViewBag.Id = id;
            return View();
        }
    }
}

