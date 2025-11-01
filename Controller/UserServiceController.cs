using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FVTIntegrationMiddleware.Controller
{
    /// <summary>
    /// Mock Internal User Service API
    /// </summary>
    [ApiController]
    [Route("internal/users")]
    public class UserServiceController : ControllerBase
    {
        private readonly ILogger<UserServiceController> _logger;

        public UserServiceController(ILogger<UserServiceController> logger)
        {
            _logger = logger;
        }

        [HttpPost("create")]
        public IActionResult CreateUser([FromBody] CreateUserRequest request)
        {
            _logger.LogInformation("Internal API: Creating user {Email}", request.Email);

            if (string.IsNullOrEmpty(request.Email))
                return BadRequest(new { error = "Email is required" });

            var response = new
            {
                userId = Guid.NewGuid().ToString(),
                email = request.Email,
                fullName = request.FullName,
                country = request.Country,
                status = "active",
                createdAt = DateTime.UtcNow
            };

            return Ok(response);
        }

        [HttpGet("{userId}")]
        public IActionResult GetUser(string userId)
        {
            _logger.LogInformation("Internal API: Getting user {UserId}", userId);

            var user = new
            {
                userId = userId,
                email = "user@example.com",
                fullName = "Ahmad Ali",
                country = "USA",
                phoneNumber = "1234567890",
                status = "active",
                joinedDate = DateTime.UtcNow.AddMonths(-6)
            };

            return Ok(user);
        }

        [HttpPut("{userId}")]
        public IActionResult UpdateUser(string userId, [FromBody] UpdateUserRequest request)
        {
            _logger.LogInformation("Internal API: Updating user {UserId}", userId);

            var response = new
            {
                userId = userId,
                email = request.Email,
                fullName = request.FullName,
                country = request.Country,
                updatedAt = DateTime.UtcNow,
                message = "User updated successfully"
            };

            return Ok(response);
        }
    }

    public class CreateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }

    public class UpdateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }
}
