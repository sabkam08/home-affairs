using HomeAffairs.Application.Abstractions.Identity;

namespace HomeAffairs.Api.Identity;

internal sealed class HttpRequestContext(
    IHttpContextAccessor httpContextAccessor)
    : IRequestContext
{
    private HttpContext? HttpContext =>
        httpContextAccessor.HttpContext;

    public string? IpAddress =>
        HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent
    {
        get
        {
            var userAgent =
                HttpContext?.Request.Headers.UserAgent
                    .ToString();

            if (string.IsNullOrWhiteSpace(userAgent))
            {
                return null;
            }

            return userAgent.Length <= 1000
                ? userAgent
                : userAgent[..1000];
        }
    }

    public string? CorrelationId =>
        HttpContext?.TraceIdentifier;
}
