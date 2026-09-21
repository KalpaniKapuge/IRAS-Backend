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

        WaitForEmailInput();
        WaitForPasswordInput();
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

        WaitForEmailInput().SendKeys(Settings.AdminEmail);
        WaitForPasswordInput().SendKeys(Settings.AdminPassword);
        Driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        WaitFor(By.CssSelector("[data-testid='dashboard'], main, nav"), 15);

        Assert.DoesNotContain("/login", Driver.Url, StringComparison.OrdinalIgnoreCase);
    }

    private IWebElement WaitForEmailInput()
    {
        return WaitForAnyInput(input =>
            Is(input, "type", "email")
            || Is(input, "name", "email")
            || Is(input, "id", "email")
            || Is(input, "autocomplete", "username")
            || Contains(input, "placeholder", "email")
            || (!Is(input, "type", "password") && !Contains(input, "autocomplete", "one-time-code")));
    }

    private IWebElement WaitForPasswordInput()
    {
        return WaitForAnyInput(input =>
            Is(input, "type", "password")
            || Is(input, "name", "password")
            || Is(input, "id", "password")
            || Is(input, "autocomplete", "current-password"));
    }

    private static bool Is(IWebElement element, string attribute, string expected) =>
        string.Equals(element.GetAttribute(attribute), expected, StringComparison.OrdinalIgnoreCase);

    private static bool Contains(IWebElement element, string attribute, string expected) =>
        element.GetAttribute(attribute)?.Contains(expected, StringComparison.OrdinalIgnoreCase) == true;
}
