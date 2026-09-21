namespace IRAS.E2ETests.Support;

public sealed class E2ETestSettings
{
    public string? BaseUrl { get; }
    public string? AdminEmail { get; }
    public string? AdminPassword { get; }
    public bool Headless { get; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl);

    public E2ETestSettings()
    {
        BaseUrl = Environment.GetEnvironmentVariable("IRAS_E2E_BASE_URL");
        AdminEmail = Environment.GetEnvironmentVariable("IRAS_E2E_ADMIN_EMAIL");
        AdminPassword = Environment.GetEnvironmentVariable("IRAS_E2E_ADMIN_PASSWORD");
        Headless = !string.Equals(Environment.GetEnvironmentVariable("IRAS_E2E_HEADLESS"), "false", StringComparison.OrdinalIgnoreCase);
    }

    public Uri BuildUri(string path)
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            throw new InvalidOperationException("Set IRAS_E2E_BASE_URL before running E2E tests.");
        }

        var baseUri = new Uri(BaseUrl.EndsWith('/') ? BaseUrl : $"{BaseUrl}/");
        return new Uri(baseUri, path.TrimStart('/'));
    }
}
