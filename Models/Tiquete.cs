using Microsoft.AspNetCore.Mvc;

namespace HelpDeskTI.Web.Models
{
    public class Tiquete : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
