using FVTIntegrationMiddleware.Configuration;
using Microsoft.Extensions.Options;

namespace FVTIntegrationMiddleware.Services
{
    public class ProxyRuleEngine : IProxyRuleEngine
    {
        private readonly ProxyConfiguration _config;
        private readonly ILogger<ProxyRuleEngine> _logger;

        public ProxyRuleEngine(
            IOptions<ProxyConfiguration> config,
            ILogger<ProxyRuleEngine> logger)
        {
            _config = config.Value;
            _logger = logger;
        }

        public RouteConfiguration? GetRouteConfiguration(PathString path)
        {
            var matchedRoute = _config.Routes.FirstOrDefault(route =>
                PathMatches(path.Value ?? string.Empty, route.MatchPath));

            if (matchedRoute != null)
            {
                _logger.LogDebug(
                    "Route matched: {RouteId} for path: {Path}",
                    matchedRoute.RouteId,
                    path);
            }

            return matchedRoute;
        }

        public bool IsIncomingRoute(PathString path)
        {
            var route = GetRouteConfiguration(path);
            return route?.Direction == ProxyDirection.Incoming;
        }

        public bool IsOutgoingRoute(PathString path)
        {
            var route = GetRouteConfiguration(path);
            return route?.Direction == ProxyDirection.Outgoing;
        }

        private bool PathMatches(string actualPath, string pattern)
        {
            // Handle wildcard patterns like /api/internal/{**catch-all}
            if (pattern.Contains("{**"))
            {
                var basePattern = pattern.Substring(0, pattern.IndexOf("{**"));
                return actualPath.StartsWith(basePattern, StringComparison.OrdinalIgnoreCase);
            }

            // Handle single segment wildcards like /api/{id}/details
            if (pattern.Contains("{") && pattern.Contains("}"))
            {
                var regex = ConvertPatternToRegex(pattern);
                return System.Text.RegularExpressions.Regex.IsMatch(actualPath, regex);
            }

            // Exact match
            return actualPath.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }

        private string ConvertPatternToRegex(string pattern)
        {
            // Convert route pattern to regex
            // Example: /api/{id}/details -> ^/api/[^/]+/details$
            var regex = pattern;
            regex = System.Text.RegularExpressions.Regex.Replace(regex, @"\{[^}]+\}", "[^/]+");
            return "^" + regex + "$";
        }
    }
}
