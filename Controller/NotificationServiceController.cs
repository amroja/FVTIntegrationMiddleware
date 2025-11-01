using Microsoft.AspNetCore.Mvc;

namespace FVTIntegrationMiddleware.Controller
{
    /// <summary>
    /// Mock External Notification Service API
    /// </summary>
    [ApiController]
    [Route("external/notifications")]
    public class NotificationServiceController : ControllerBase
    {
        private readonly ILogger<NotificationServiceController> _logger;

        public NotificationServiceController(ILogger<NotificationServiceController> logger)
        {
            _logger = logger;
        }

        [HttpPost("send")]
        public IActionResult SendNotification([FromBody] NotificationRequest request)
        {
            _logger.LogInformation("External Notification API: Sending {Type} to {Recipient}",
                request.Type, request.Recipient);


            if (!Request.Headers.ContainsKey("X-API-Key"))
            {
                return Unauthorized(new { error = "X-API-Key header is required" });
            }


            var response = new
            {
                notificationId = $"NOT-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                type = request.Type,
                recipient = request.Recipient,
                status = "sent",
                sentAt = DateTime.UtcNow,
                deliveryEstimate = DateTime.UtcNow.AddSeconds(30)
            };

            return Ok(response);
        }

        [HttpPost("send-bulk")]
        public IActionResult SendBulkNotifications([FromBody] BulkNotificationRequest request)
        {
            _logger.LogInformation("External Notification API: Sending bulk notifications to {Count} recipients",
                request.Recipients?.Count ?? 0);

            var results = request.Recipients?.Select((r, i) => new
            {
                notificationId = $"NOT-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                recipient = r,
                status = "queued",
                queuePosition = i + 1
            }).ToList();

            var response = new
            {
                batchId = $"BATCH-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                totalRecipients = request.Recipients?.Count ?? 0,
                status = "processing",
                results = results,
                queuedAt = DateTime.UtcNow
            };

            return Ok(response);
        }

        [HttpGet("status/{notificationId}")]
        public IActionResult GetNotificationStatus(string notificationId)
        {
            _logger.LogInformation("External Notification API: Getting status for {NotificationId}", notificationId);

            var status = new
            {
                notificationId = notificationId,
                status = "delivered",
                sentAt = DateTime.UtcNow.AddMinutes(-15),
                deliveredAt = DateTime.UtcNow.AddMinutes(-14),
                readAt = DateTime.UtcNow.AddMinutes(-10)
            };

            return Ok(status);
        }
    }

    public class NotificationRequest
    {
        public string Type { get; set; } = string.Empty; // email, sms, push
        public string Recipient { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class BulkNotificationRequest
    {
        public string Type { get; set; } = string.Empty;
        public List<string>? Recipients { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
