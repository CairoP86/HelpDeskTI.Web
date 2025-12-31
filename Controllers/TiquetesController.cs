using Microsoft.AspNetCore.Mvc;

namespace HelpDeskTI.Web.Controllers
{
    public class TiquetesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
