using Azure.Core;
using EMenu.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    public class QrController : Controller
    {
        private readonly QrService _service;

        public QrController(QrService service)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Table(int tableId)
        {
            var url = $"{Request.Scheme}://{Request.Host}/Customer/Start?tableId={tableId}";

            var qr = _service.Generate(url);

            return File(qr, "image/png");
        }
    }
}
