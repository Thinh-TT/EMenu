using EMenu.Application.Services;
using EMenu.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    public class OrderPageController : Controller
    {
        private readonly SessionService _sessionService;
        private readonly CheckoutRequestTracker _checkoutRequestTracker;

        public OrderPageController(
            SessionService sessionService,
            CheckoutRequestTracker checkoutRequestTracker)
        {
            _sessionService = sessionService;
            _checkoutRequestTracker = checkoutRequestTracker;
        }

        public IActionResult Tracking(int sessionId)
        {
            var session = _sessionService.GetById(sessionId);

            if (session == null)
                return RedirectToAction("Index", "Menu");

            ViewBag.SessionId = sessionId;
            ViewBag.TableId = session.TableID;
            ViewBag.CheckoutRequested = _checkoutRequestTracker.HasRequestForSession(sessionId);
            return View();
        }
    }
}
