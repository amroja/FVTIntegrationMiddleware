namespace FVTIntegrationMiddleware.Configuration
{
    public class RouteConfiguration
    {
        public string RouteId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string MatchPath { get; set; } = string.Empty;
        public string DestinationPrefix { get; set; } = string.Empty;
        public ProxyDirection Direction { get; set; }
        public List<TransformationRule> Transformations { get; set; } = new();
        public List<TransformationRule> ResponseTransformations { get; set; } = new();
    }
}
