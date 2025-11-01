using FVTIntegrationMiddleware.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FVTIntegrationMiddleware.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly ProxyConfiguration _config;

        public HealthController(
            ILogger<HealthController> logger,
            IOptions<ProxyConfiguration> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                Status = "Healthy",
                Service = "FVT Integration Middleware",
                Version = "1.0.0",
                Timestamp = DateTime.UtcNow,
                ConfiguredRoutes = _config.Routes.Count
            });
        }

        [HttpGet("routes")]
        public IActionResult GetRoutes()
        {
            var routes = _config.Routes.Select(r => new
            {
                r.RouteId,
                r.Description,
                r.MatchPath,
                r.DestinationPrefix,
                Direction = r.Direction.ToString(),
                TransformationCount = r.Transformations.Count,
                ResponseTransformationCount = r.ResponseTransformations.Count
            });

            return Ok(routes);
        }

        [HttpGet("routes/{routeId}")]
        public IActionResult GetRoute(string routeId)
        {
            var route = _config.Routes.FirstOrDefault(r =>
                r.RouteId.Equals(routeId, StringComparison.OrdinalIgnoreCase));

            if (route == null)
                return NotFound(new { Message = $"Route '{routeId}' not found" });

            return Ok(route);
        }
    }
}
