using EMenu.Application.Services;
using EMenu.Domain.Constants;
using EMenu.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    [ApiController]
    [Route("api/session")]
    [Authorize(Roles = AppRoles.AdminOrStaff)]
    public class SessionController : ControllerBase
    {
        private readonly SessionService _sessionService;
        private readonly ILogger<SessionController> _logger;

        public SessionController(
            SessionService sessionService,
            ILogger<SessionController> logger)
        {
            _sessionService = sessionService;
            _logger = logger;
        }

        [HttpPost("start")]
        public IActionResult Start(int tableId, int customerId)
        {
            try
            {
                var session = _sessionService.StartSession(tableId, customerId);

                _logger.LogInformation(
                    "Session started by user {UserId} ({Username}) roles {Roles}: table {TableId}, customer {CustomerId}, session {SessionId}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    tableId,
                    customerId,
                    session.OrderSessionID);

                return Ok(session);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Session start failed for user {UserId} ({Username}) roles {Roles}: table {TableId}, customer {CustomerId}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    tableId,
                    customerId);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("end")]
        public IActionResult EndSession(int tableId)
        {
            try
            {
                _sessionService.EndSessionByTable(tableId);

                _logger.LogInformation(
                    "Session ended by user {UserId} ({Username}) roles {Roles}: table {TableId}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    tableId);

                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Session end failed for user {UserId} ({Username}) roles {Roles}: table {TableId}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    tableId);
                return BadRequest(ex.Message);
            }
        }
    }
}
