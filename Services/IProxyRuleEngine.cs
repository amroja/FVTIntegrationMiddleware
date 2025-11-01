using FVTIntegrationMiddleware.Configuration;

namespace FVTIntegrationMiddleware.Services
{
    public interface IProxyRuleEngine
    {
        RouteConfiguration? GetRouteConfiguration(PathString path);
        bool IsIncomingRoute(PathString path);
        bool IsOutgoingRoute(PathString path);
    }
}
