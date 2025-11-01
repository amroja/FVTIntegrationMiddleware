using FVTIntegrationMiddleware.Configuration;
using FVTIntegrationMiddleware.Services;
using System.Text;

namespace FVTIntegrationMiddleware.Middleware
{
    public class ResponseTransformationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ResponseTransformationMiddleware> _logger;
        private readonly IProxyRuleEngine _ruleEngine;
        private readonly ITransformationService _transformationService;

        public ResponseTransformationMiddleware(
            RequestDelegate next,
            ILogger<ResponseTransformationMiddleware> logger,
            IProxyRuleEngine ruleEngine,
            ITransformationService transformationService)
        {
            _next = next;
            _logger = logger;
            _ruleEngine = ruleEngine;
            _transformationService = transformationService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            await _next(context);

            var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();
            var routeConfig = _ruleEngine.GetRouteConfiguration(context.Request.Path);

            if (routeConfig?.ResponseTransformations?.Any() == true)
            {
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                var modifiedBody = await ApplyResponseTransformations(
                    responseBody,
                    routeConfig,
                    requestId);

                var modifiedBytes = Encoding.UTF8.GetBytes(modifiedBody);
                context.Response.Body = new MemoryStream();
                await context.Response.Body.WriteAsync(modifiedBytes);
                context.Response.ContentLength = modifiedBytes.Length;
                context.Response.Body.Seek(0, SeekOrigin.Begin);
            }

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            await context.Response.Body.CopyToAsync(originalBodyStream);
        }

        private async Task<string> ApplyResponseTransformations(
            string responseBody,
            RouteConfiguration routeConfig,
            string requestId)
        {
            var modifiedBody = responseBody;

            foreach (var transformation in routeConfig.ResponseTransformations)
            {
                try
                {
                    switch (transformation.Type)
                    {
                        case "InjectMetadata":
                            modifiedBody = InjectMetadata(modifiedBody, transformation, requestId);
                            break;

                        case "ModifyJsonValue":
                            modifiedBody = _transformationService.ModifyJsonValue(
                                modifiedBody,
                                transformation.JsonPath!,
                                transformation.OldValue,
                                transformation.NewValue!);
                            break;

                        default:
                            _logger.LogWarning(
                                "[{RequestId}] Unknown response transformation type: {Type}",
                                requestId,
                                transformation.Type);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[{RequestId}] Error applying response transformation {Type}",
                        requestId,
                        transformation.Type);
                }
            }

            return modifiedBody;
        }

        private string InjectMetadata(string responseBody, TransformationRule rule, string requestId)
        {
            if (string.IsNullOrWhiteSpace(responseBody) || rule.Metadata == null)
                return responseBody;

            var processedMetadata = new Dictionary<string, object>();
            foreach (var kvp in rule.Metadata)
            {
                var value = kvp.Value.ToString();
                if (value == "{{timestamp}}")
                {
                    processedMetadata[kvp.Key] = DateTime.UtcNow.ToString("o");
                }
                else
                {
                    processedMetadata[kvp.Key] = kvp.Value;
                }
            }

            var modifiedBody = _transformationService.InjectMetadata(
                responseBody,
                rule.JsonPath!,
                processedMetadata);

            _logger.LogInformation(
                "[{RequestId}] Metadata injected into response at path: {JsonPath}",
                requestId,
                rule.JsonPath);

            return modifiedBody;
        }
    }
}
