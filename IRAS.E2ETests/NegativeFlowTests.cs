using IRAS.E2ETests.Support;

namespace IRAS.E2ETests;

public sealed class NegativeFlowTests : E2ETestBase
{
    public NegativeFlowTests(BrowserFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public void Wrong_password_shows_login_error()
    {
        if (SkipWhenNotConfigured())
        {
            return;
        }

        ClearBrowserState();
        GoTo("/login");

        TypeIntoField("not-a-real-user@example.com", "email");
        TypeIntoField("WrongPassword123!", "password");
        SubmitCurrentForm();

        WaitForBodyContains("Invalid");
    }

    [Fact]
    public void Protected_admin_page_redirects_to_login_when_signed_out()
    {
        if (SkipWhenNotConfigured())
        {
            return;
        }

        ClearBrowserState();
        GoTo("/admin");

        var signedOut = Driver.Url.Contains("/login", StringComparison.OrdinalIgnoreCase)
            || BodyContains("Sign in")
            || BodyContains("Welcome back");

        Assert.True(signedOut, DescribeCurrentPage());
    }
}
