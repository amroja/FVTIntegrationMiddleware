using Microsoft.AspNetCore.Mvc;

namespace FVTIntegrationMiddleware.Controller
{
    [ApiController]
    [Route("external/payments")]
    public class PaymentServiceController : ControllerBase
    {
        private readonly ILogger<PaymentServiceController> _logger;

        public PaymentServiceController(ILogger<PaymentServiceController> logger)
        {
            _logger = logger;
        }

        [HttpPost("process")]
        public IActionResult ProcessPayment([FromBody] PaymentRequest request)
        {
            _logger.LogInformation("External Payment API: Processing payment for {Amount}", request.Amount);

            if (!Request.Headers.ContainsKey("Authorization"))
            {
                return Unauthorized(new { error = "Authorization header is required" });
            }

            if (request.CreditCard?.Contains("*") != true)
            {
                _logger.LogWarning("Credit card not masked!");
            }

            var response = new
            {
                transactionId = $"TXN-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                status = "success",
                amount = request.Amount,
                currency = request.Currency,
                processedAt = DateTime.UtcNow,
                cardLastFour = request.CreditCard?.Substring(Math.Max(0, request.CreditCard.Length - 4))
            };

            return Ok(response);
        }

        [HttpGet("transaction/{transactionId}")]
        public IActionResult GetTransaction(string transactionId)
        {
            _logger.LogInformation("External Payment API: Getting transaction {TransactionId}", transactionId);

            var transaction = new
            {
                transactionId = transactionId,
                status = "completed",
                amount = 250.00,
                currency = "USD",
                processedAt = DateTime.UtcNow.AddMinutes(-30),
                cardLastFour = "3456"
            };

            return Ok(transaction);
        }

        [HttpPost("refund")]
        public IActionResult RefundPayment([FromBody] RefundRequest request)
        {
            _logger.LogInformation("External Payment API: Refunding transaction {TransactionId}", request.TransactionId);

            var response = new
            {
                refundId = $"REF-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                transactionId = request.TransactionId,
                amount = request.Amount,
                status = "refunded",
                refundedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
    }

    public class PaymentRequest
    {
        public string CreditCard { get; set; } = string.Empty;
        public string CVV { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string CardholderName { get; set; } = string.Empty;
    }

    public class RefundRequest
    {
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
