namespace FVTIntegrationMiddleware.Services
{
    public interface ITransformationService
    {
        string ModifyJsonValue(string json, string jsonPath, string? oldValue, string newValue);
        string InjectMetadata(string json, string jsonPath, Dictionary<string, object> metadata);
        string MaskSensitiveFields(string json, List<string> jsonPaths);
        string RemoveJsonField(string json, string jsonPath);
        string RenameJsonField(string json, string oldFieldName, string newFieldName);
    }
}
