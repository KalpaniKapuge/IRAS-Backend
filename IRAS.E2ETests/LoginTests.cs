using IRAS.E2ETests.Support;
using OpenQA.Selenium;

namespace IRAS.E2ETests;

public sealed class LoginTests : E2ETestBase
{
    public LoginTests(BrowserFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public void Admin_can_open_login_page()
    {
        if (SkipWhenNotConfigured())
        {
            return;
        }

        GoTo("/login");

        WaitFor(By.CssSelector("input[type='email'], input[name='email'], input[autocomplete='username']"));
        WaitFor(By.CssSelector("input[type='password'], input[name='password'], input[autocomplete='current-password']"));
    }

    [Fact]
    public void Admin_can_login_when_credentials_are_configured()
    {
        if (SkipWhenNotConfigured() || string.IsNullOrWhiteSpace(Settings.AdminEmail) || string.IsNullOrWhiteSpace(Settings.AdminPassword))
        {
            Console.WriteLine("Skipped: set IRAS_E2E_ADMIN_EMAIL and IRAS_E2E_ADMIN_PASSWORD to run login test.");
            return;
        }

        GoTo("/login");

        Driver.FindElement(By.CssSelector("input[type='email'], input[name='email'], input[autocomplete='username']")).SendKeys(Settings.AdminEmail);
        Driver.FindElement(By.CssSelector("input[type='password'], input[name='password'], input[autocomplete='current-password']")).SendKeys(Settings.AdminPassword);
        Driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        WaitFor(By.CssSelector("[data-testid='dashboard'], main, nav"), 15);

        Assert.DoesNotContain("/login", Driver.Url, StringComparison.OrdinalIgnoreCase);
    }
}
