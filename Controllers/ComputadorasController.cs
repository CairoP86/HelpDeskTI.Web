using Microsoft.AspNetCore.Mvc;

namespace HelpDeskTI.Web.Controllers
{
    public class ComputadorasController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
