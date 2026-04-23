using EMenu.Application.Services;
using EMenu.Domain.Constants;
using EMenu.Web.Extensions;
using EMenu.Web.ViewModels;
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

        [HttpPost("transfer")]
        public IActionResult Transfer([FromBody] SessionTableActionRequest request)
        {
            if (!IsValidRequest(request))
                return BadRequest("SourceTableId and TargetTableId are required.");

            var actor = ResolveActor(request.Actor);

            try
            {
                var result = _sessionService.TransferTable(request.SourceTableId, request.TargetTableId);

                _logger.LogInformation(
                    "Table transfer succeeded at {OccurredAt} by user {UserId} ({Username}) roles {Roles} actor {Actor}: source table {SourceTableId}, target table {TargetTableId}, source session {SourceSessionId}, target session {TargetSessionId}, moved orders {MovedOrderCount}.",
                    DateTime.Now,
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    actor,
                    result.SourceTableId,
                    result.TargetTableId,
                    result.SourceSessionId,
                    result.TargetSessionId,
                    result.MovedOrderCount);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Table transfer failed at {OccurredAt} by user {UserId} ({Username}) roles {Roles} actor {Actor}: source table {SourceTableId}, target table {TargetTableId}.",
                    DateTime.Now,
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    actor,
                    request.SourceTableId,
                    request.TargetTableId);

                return BadRequest(ex.Message);
            }
        }

        [HttpPost("merge")]
        public IActionResult Merge([FromBody] SessionTableActionRequest request)
        {
            if (!IsValidRequest(request))
                return BadRequest("SourceTableId and TargetTableId are required.");

            var actor = ResolveActor(request.Actor);

            try
            {
                var result = _sessionService.MergeTable(request.SourceTableId, request.TargetTableId);

                _logger.LogInformation(
                    "Table merge succeeded at {OccurredAt} by user {UserId} ({Username}) roles {Roles} actor {Actor}: source table {SourceTableId}, target table {TargetTableId}, source session {SourceSessionId}, target session {TargetSessionId}, moved orders {MovedOrderCount}.",
                    DateTime.Now,
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    actor,
                    result.SourceTableId,
                    result.TargetTableId,
                    result.SourceSessionId,
                    result.TargetSessionId,
                    result.MovedOrderCount);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Table merge failed at {OccurredAt} by user {UserId} ({Username}) roles {Roles} actor {Actor}: source table {SourceTableId}, target table {TargetTableId}.",
                    DateTime.Now,
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    actor,
                    request.SourceTableId,
                    request.TargetTableId);

                return BadRequest(ex.Message);
            }
        }

        private static bool IsValidRequest(SessionTableActionRequest? request)
        {
            return request != null &&
                   request.SourceTableId > 0 &&
                   request.TargetTableId > 0;
        }

        private string ResolveActor(string? requestActor)
        {
            if (!string.IsNullOrWhiteSpace(requestActor))
                return requestActor.Trim();

            var userName = User.GetAuditUserName();
            var userId = User.GetAuditUserId();

            return $"{userName} ({userId})";
        }
    }
}
