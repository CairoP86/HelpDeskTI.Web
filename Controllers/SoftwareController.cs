using Microsoft.AspNetCore.Mvc;

namespace HelpDeskTI.Web.Controllers
{
    public class SoftwareController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
