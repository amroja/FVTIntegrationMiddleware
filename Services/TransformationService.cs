using Newtonsoft.Json.Linq;

namespace FVTIntegrationMiddleware.Services
{
    public class TransformationService : ITransformationService
    {
        private readonly ILogger<TransformationService> _logger;

        public TransformationService(ILogger<TransformationService> logger)
        {
            _logger = logger;
        }

        public string ModifyJsonValue(string json, string jsonPath, string? oldValue, string newValue)
        {
            try
            {
                var jObject = JToken.Parse(json);
                var tokens = jObject.SelectTokens(jsonPath);

                foreach (var token in tokens)
                {
                    if (oldValue == null || token.ToString() == oldValue)
                    {
                        if (token.Parent is JProperty property)
                        {
                            property.Value = JToken.FromObject(newValue);
                        }
                    }
                }

                return jObject.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error modifying JSON value at path: {JsonPath}", jsonPath);
                return json;
            }
        }

        public string InjectMetadata(string json, string jsonPath, Dictionary<string, object> metadata)
        {
            try
            {
                var jObject = JToken.Parse(json);
                var tokens = jObject.SelectTokens(jsonPath).ToList();

                if (!tokens.Any())
                {
                    _logger.LogWarning("No tokens found at path: {JsonPath}", jsonPath);
                    return json;
                }

                foreach (var token in tokens)
                {
                    if (token is JObject targetObject)
                    {
                        foreach (var kvp in metadata)
                        {
                            targetObject[kvp.Key] = JToken.FromObject(kvp.Value);
                        }
                    }
                }

                return jObject.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error injecting metadata at path: {JsonPath}", jsonPath);
                return json;
            }
        }

        public string MaskSensitiveFields(string json, List<string> jsonPaths)
        {
            try
            {
                var jObject = JToken.Parse(json);

                foreach (var path in jsonPaths)
                {
                    var tokens = jObject.SelectTokens(path);
                    foreach (var token in tokens)
                    {
                        if (token.Parent is JProperty property)
                        {
                            var originalValue = property.Value.ToString();
                            property.Value = MaskValue(originalValue);
                        }
                    }
                }

                return jObject.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error masking sensitive fields");
                return json;
            }
        }

        private string MaskValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            if (value.Length <= 4)
                return new string('*', value.Length);

            var visibleChars = Math.Min(4, value.Length / 4);
            var maskedLength = value.Length - visibleChars;

            return new string('*', maskedLength) + value.Substring(value.Length - visibleChars);
        }
    }
}
