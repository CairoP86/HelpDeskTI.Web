using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskTI.Web.Controllers
{
    public class NotificacionesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }


    }

}
