namespace FVTIntegrationMiddleware.Configuration
{
    public class TransformationRule
    {
        public string Type { get; set; } = string.Empty;
        public string? HeaderName { get; set; }
        public string? HeaderValue { get; set; }
        public bool Required { get; set; }
        public string? JsonPath { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? ParamName { get; set; }
        public string? ParamValue { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public List<string>? JsonPaths { get; set; }
        public string? FieldToRemove { get; set; }
        public string? OldFieldName { get; set; }
        public string? NewFieldName { get; set; }
    }
}
