using IRAS.E2ETests.Support;
using OpenQA.Selenium;

namespace IRAS.E2ETests;

public sealed class SmokeTests : E2ETestBase
{
    public SmokeTests(BrowserFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public void Home_page_loads_without_server_error()
    {
        if (SkipWhenNotConfigured())
        {
            return;
        }

        GoTo("/");

        WaitFor(By.TagName("body"));
        var bodyText = Driver.FindElement(By.TagName("body")).Text;

        Assert.False(string.IsNullOrWhiteSpace(Driver.Title) && string.IsNullOrWhiteSpace(bodyText));
        Assert.DoesNotContain("404", bodyText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("500", bodyText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("application error", bodyText, StringComparison.OrdinalIgnoreCase);
    }
}
