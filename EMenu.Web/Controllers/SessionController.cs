using EMenu.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    [ApiController]
    [Route("api/session")]
    public class SessionController : ControllerBase
    {
        private readonly SessionService _sessionService;

        public SessionController(SessionService sessionService)
        {
            _sessionService = sessionService;
        }

        [HttpPost("start")]
        public IActionResult Start(int tableId, int customerId)
        {
            var session = _sessionService.StartSession(tableId, customerId);

            return Ok(session);
        }

        [HttpPost("end")]
        public IActionResult End(int sessionId)
        {
            _sessionService.EndSession(sessionId);

            return Ok();
        }
    }
}
