using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EntityFramework.Middleware;

public class Middleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<Middleware> _logger;
    private readonly List<ValidationRuleSet> _rules;

    public Middleware(RequestDelegate next, ILogger<Middleware> logger)
    {
        _next = next;
        _logger = logger;

        _logger.LogInformation("[Middleware] Initialization started.");

        var json = File.ReadAllText("example_validation_rules.json");
        _rules = JsonSerializer.Deserialize<ValidationWrapper>(json)?.Validations ?? new();

        _logger.LogInformation("[Middleware] Initialization finished.");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower();
        var method = context.Request.Method.ToUpper();

        if ((method == "POST" || method == "PUT") && path != null && path.StartsWith("/api/devices"))
        {
            _logger.LogInformation("[Middleware] Validating additionalProperties started.");

            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (!root.TryGetProperty("deviceTypeName", out var deviceTypeProp) ||
                !root.TryGetProperty("isEnabled", out var isEnabledProp) ||
                !root.TryGetProperty("additionalProperties", out var propsProp))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Missing required fields in request body.");
                _logger.LogWarning("[Middleware] Missing fields. Validation failed.");
                return;
            }

            var deviceType = deviceTypeProp.GetString();
            var isEnabled = isEnabledProp.GetBoolean();
            var additionalProps = propsProp;

            var matched = _rules.FirstOrDefault(r =>
                string.Equals(r.Type, deviceType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.PreRequestName, "isEnabled", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.PreRequestValue, isEnabled.ToString().ToLower(), StringComparison.OrdinalIgnoreCase));

            if (matched != null)
            {
                foreach (var rule in matched.Rules)
                {
                    if (!additionalProps.TryGetProperty(rule.ParamName, out var actualValue))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync($"Missing property: {rule.ParamName}");
                        _logger.LogWarning("[Middleware] Missing property: {Property}", rule.ParamName);
                        return;
                    }

                    if (rule.Regex.ValueKind == JsonValueKind.String)
                    {
                        var pattern = rule.Regex.GetString();
                        if (!Regex.IsMatch(actualValue.ToString(), pattern!))
                        {
                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                            await context.Response.WriteAsync($"Invalid format for {rule.ParamName}");
                            _logger.LogWarning("[Middleware] Regex failed for property: {Property}", rule.ParamName);
                            return;
                        }
                    }
                    else if (rule.Regex.ValueKind == JsonValueKind.Array)
                    {
                        var allowed = rule.Regex.EnumerateArray().Select(e => e.GetString()).ToHashSet();
                        if (!allowed.Contains(actualValue.ToString()))
                        {
                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                            await context.Response.WriteAsync($"Invalid value for {rule.ParamName}");
                            _logger.LogWarning("[Middleware] Value not allowed for property: {Property}", rule.ParamName);
                            return;
                        }
                    }
                }
            }

            _logger.LogInformation("[Middleware] additionalProperties validation passed.");
        }

        await _next(context);
    }

    private class ValidationWrapper
    {
        public List<ValidationRuleSet> Validations { get; set; } = new();
    }

    private class ValidationRuleSet
    {
        public string Type { get; set; } = null!;
        public string PreRequestName { get; set; } = null!;
        public string PreRequestValue { get; set; } = null!;
        public List<Rule> Rules { get; set; } = new();
    }

    private class Rule
    {
        public string ParamName { get; set; } = null!;

        [JsonPropertyName("regex")]
        public JsonElement Regex { get; set; }
    }
}