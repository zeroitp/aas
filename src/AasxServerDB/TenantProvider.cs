namespace AasxServerDB;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

public class TenantProvider
{
    private const string ProjectIdHeaderName = "x-project-id";
    private const string SubscriptionIdHeaderName = "x-subscription-id";
    private const string TenantIdHeaderName = "x-tenant-id";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public TenantProvider(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public string TenantId => _httpContextAccessor?
        .HttpContext?
        .Request?
        .Headers[TenantIdHeaderName];
    public string ProjectId => _httpContextAccessor?
        .HttpContext?
        .Request?
        .Headers[ProjectIdHeaderName];
    public string SubscriptionId => _httpContextAccessor?
        .HttpContext?
        .Request?
        .Headers[SubscriptionIdHeaderName];

    public string GetAASConnectionString()
    {
        return BuildConnectionString(_configuration["DatabaseConnection:ConnectionString"], ProjectId);
    }

    public string GetAHIConnectionString()
    {
        return BuildConnectionString(_configuration["DatabaseConnection:ConnectionString__AHI"], ProjectId);
    }

    public static string BuildConnectionString(string rawConnectionString, string targetId)
    {
        return rawConnectionString.Replace("{{projectid}}", targetId.Replace("-", ""));
    }
}
