using FVTIntegrationMiddleware.Configuration;
using FVTIntegrationMiddleware.Services;
using System.Text;

namespace FVTIntegrationMiddleware.Middleware
{
    public class RequestTransformationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestTransformationMiddleware> _logger;
        private readonly IProxyRuleEngine _ruleEngine;
        private readonly ITransformationService _transformationService;

        public RequestTransformationMiddleware(
            RequestDelegate next,
            ILogger<RequestTransformationMiddleware> logger,
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
            var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();
            var routeConfig = _ruleEngine.GetRouteConfiguration(context.Request.Path);

            if (routeConfig != null)
            {
                _logger.LogInformation(
                    "[{RequestId}] Route matched: {RouteId} - Direction: {Direction}",
                    requestId,
                    routeConfig.RouteId,
                    routeConfig.Direction);
                await ApplyTransformations(context, routeConfig, requestId);
            }

            await _next(context);
        }

        private async Task ApplyTransformations(
            HttpContext context,
            RouteConfiguration routeConfig,
            string requestId)
        {
            foreach (var transformation in routeConfig.Transformations)
            {
                try
                {
                    switch (transformation.Type)
                    {
                        case "ValidateHeader":
                            ValidateHeader(context, transformation, requestId);
                            break;

                        case "AddHeader":
                            AddHeader(context, transformation, requestId);
                            break;

                        case "ModifyJsonBody":
                            await ModifyJsonBody(context, transformation, requestId);
                            break;

                        case "ModifyQueryParam":
                            ModifyQueryParam(context, transformation, requestId);
                            break;

                        case "MaskSensitiveData":
                            await MaskSensitiveData(context, transformation, requestId);
                            break;
                        case "RemoveJsonField":
                            await RemoveJsonField(context, transformation, requestId);
                            break;
                        case "RenameJsonField":
                            await RenameJsonField(context, transformation, requestId);
                            break;

                        default:
                            _logger.LogWarning(
                                "[{RequestId}] Unknown transformation type: {Type}",
                                requestId,
                                transformation.Type);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[{RequestId}] Error applying transformation {Type}",
                        requestId,
                        transformation.Type);
                }
            }
        }

        private void ValidateHeader(HttpContext context, TransformationRule rule, string requestId)
        {
            if (rule.Required && !context.Request.Headers.ContainsKey(rule.HeaderName!))
            {
                _logger.LogWarning(
                    "[{RequestId}] Required header missing: {HeaderName}",
                    requestId,
                    rule.HeaderName);

                context.Response.StatusCode = 400;
                throw new InvalidOperationException($"Required header '{rule.HeaderName}' is missing");
            }

            _logger.LogInformation(
                "[{RequestId}] Header validated: {HeaderName}",
                requestId,
                rule.HeaderName);
        }

        private void AddHeader(HttpContext context, TransformationRule rule, string requestId)
        {
            context.Request.Headers[rule.HeaderName!] = rule.HeaderValue;

            _logger.LogInformation(
                "[{RequestId}] Header added: {HeaderName} = {HeaderValue}",
                requestId,
                rule.HeaderName,
                rule.HeaderValue);
        }

        private async Task ModifyJsonBody(HttpContext context, TransformationRule rule, string requestId)
        {
            if (context.Request.ContentLength == 0)
                return;

            context.Request.EnableBuffering();
            context.Request.Body.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
                return;

            var modifiedBody = _transformationService.ModifyJsonValue(
                body,
                rule.JsonPath!,
                rule.OldValue,
                rule.NewValue!);

            var newBodyBytes = Encoding.UTF8.GetBytes(modifiedBody);
            var newBodyStream = new MemoryStream(newBodyBytes);

            context.Request.Body = newBodyStream;
            context.Request.ContentLength = newBodyBytes.Length;

            _logger.LogInformation(
                "[{RequestId}] JSON body modified at path: {JsonPath}",
                requestId,
                rule.JsonPath);
        }

        private void ModifyQueryParam(HttpContext context, TransformationRule rule, string requestId)
        {
            var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(
                context.Request.QueryString.Value ?? string.Empty);

            var newParams = new Dictionary<string, string>(queryParams.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToString()));

            newParams[rule.ParamName!] = rule.ParamValue!;

            var newQueryString = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(
                string.Empty,
                newParams!);

            context.Request.QueryString = new QueryString(newQueryString);

            _logger.LogInformation(
                "[{RequestId}] Query parameter modified: {ParamName} = {ParamValue}",
                requestId,
                rule.ParamName,
                rule.ParamValue);
        }

        private async Task MaskSensitiveData(HttpContext context, TransformationRule rule, string requestId)
        {
            if (context.Request.ContentLength == 0 || rule.JsonPaths == null)
                return;

            context.Request.EnableBuffering();
            context.Request.Body.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
                return;

            var maskedBody = _transformationService.MaskSensitiveFields(body, rule.JsonPaths);

            var newBodyBytes = Encoding.UTF8.GetBytes(maskedBody);
            var newBodyStream = new MemoryStream(newBodyBytes);

            context.Request.Body = newBodyStream;
            context.Request.ContentLength = newBodyBytes.Length;

            _logger.LogInformation(
                "[{RequestId}] Sensitive data masked in fields: {Fields}",
                requestId,
                string.Join(", ", rule.JsonPaths));
        }


        private async Task RemoveJsonField(HttpContext context, TransformationRule rule, string requestId)
        {
            if (context.Request.ContentLength == 0 || string.IsNullOrWhiteSpace(rule.FieldToRemove))
                return;

            context.Request.EnableBuffering();
            context.Request.Body.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
                return;

            var modifiedBody = _transformationService.RemoveJsonField(body, rule.FieldToRemove);

            var newBodyBytes = Encoding.UTF8.GetBytes(modifiedBody);
            var newBodyStream = new MemoryStream(newBodyBytes);

            context.Request.Body = newBodyStream;
            context.Request.ContentLength = newBodyBytes.Length;

            _logger.LogInformation(
                "[{RequestId}] JSON field removed: {FieldName}",
                requestId,
                rule.FieldToRemove);
        }

        private async Task RenameJsonField(HttpContext context, TransformationRule rule, string requestId)
        {
            if (context.Request.ContentLength == 0 ||
                string.IsNullOrWhiteSpace(rule.OldFieldName) ||
                string.IsNullOrWhiteSpace(rule.NewFieldName))
                return;

            context.Request.EnableBuffering();
            context.Request.Body.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
                return;

            var modifiedBody = _transformationService.RenameJsonField(
                body,
                rule.OldFieldName,
                rule.NewFieldName);

            var newBodyBytes = Encoding.UTF8.GetBytes(modifiedBody);
            var newBodyStream = new MemoryStream(newBodyBytes);

            context.Request.Body = newBodyStream;
            context.Request.ContentLength = newBodyBytes.Length;

            _logger.LogInformation(
                "[{RequestId}] JSON field renamed: {OldName} → {NewName}",
                requestId,
                rule.OldFieldName,
                rule.NewFieldName);
        }
    }
}
