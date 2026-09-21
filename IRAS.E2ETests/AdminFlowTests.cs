using IRAS.E2ETests.Support;
using OpenQA.Selenium;

namespace IRAS.E2ETests;

public sealed class AdminFlowTests : E2ETestBase
{
    public AdminFlowTests(BrowserFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public void Admin_can_navigate_core_admin_pages_and_logout()
    {
        if (SkipWhenNotConfigured() || string.IsNullOrWhiteSpace(Settings.AdminEmail) || string.IsNullOrWhiteSpace(Settings.AdminPassword))
        {
            return;
        }

        Login(Settings.AdminEmail, Settings.AdminPassword);
        Assert.True(BodyContains("Dashboard") || BodyContains("Platform Overview"), DescribeCurrentPage());
        AssertNoServerError();

        ClickByText("Users");
        WaitForBodyContains("Users");
        AssertNoServerError();

        ClickByText("Job Postings", "Jobs");
        WaitForBodyContains("Job");
        AssertNoServerError();

        ClickByText("Skill Taxonomy", "Skills");
        WaitForBodyContains("Skill");
        AssertNoServerError();

        ClickByText("System Status", "Status");
        WaitForBodyContains("Status");
        AssertNoServerError();

        ClickByText("Sign out", "Logout", "Log out");
        var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(30));
        wait.Until(_ => Driver.Url.Contains("/login", StringComparison.OrdinalIgnoreCase)
            || BodyContains("Sign in")
            || BodyContains("Welcome back"));
    }
}
