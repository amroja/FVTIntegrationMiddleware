using System.Diagnostics;
using System.Text;

namespace FVTIntegrationMiddleware.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestId = Guid.NewGuid().ToString();
            context.Items["RequestId"] = requestId;

            var stopwatch = Stopwatch.StartNew();

            // Log request
            await LogRequest(context, requestId);

            // Capture original response body stream
            var originalBodyStream = context.Response.Body;

            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);

                stopwatch.Stop();

                // Log response
                await LogResponse(context, requestId, stopwatch.ElapsedMilliseconds);

                // Copy response back to original stream
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private async Task LogRequest(HttpContext context, string requestId)
        {
            context.Request.EnableBuffering();

            var request = context.Request;
            var requestBody = string.Empty;

            if (request.ContentLength > 0)
            {
                request.Body.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(
                    request.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true);

                requestBody = await reader.ReadToEndAsync();
                request.Body.Seek(0, SeekOrigin.Begin);
            }

            _logger.LogInformation(
                "REQUEST [{RequestId}] {Method} {Path} {QueryString}\n" +
                "Headers: {Headers}\n" +
                "Body: {Body}",
                requestId,
                request.Method,
                request.Path,
                request.QueryString,
                string.Join(", ", request.Headers.Select(h => $"{h.Key}={h.Value}")),
                string.IsNullOrEmpty(requestBody) ? "(empty)" : requestBody);
        }

        private async Task LogResponse(HttpContext context, string requestId, long elapsedMs)
        {
            var response = context.Response;
            var responseBody = string.Empty;

            if (response.Body.CanSeek)
            {
                response.Body.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
                responseBody = await reader.ReadToEndAsync();
                response.Body.Seek(0, SeekOrigin.Begin);
            }

            _logger.LogInformation(
                "RESPONSE [{RequestId}] Status: {StatusCode} Duration: {Duration}ms\n" +
                "Headers: {Headers}\n" +
                "Body: {Body}",
                requestId,
                response.StatusCode,
                elapsedMs,
                string.Join(", ", response.Headers.Select(h => $"{h.Key}={h.Value}")),
                string.IsNullOrEmpty(responseBody) ? "(empty)" : responseBody);
        }
    }
}
