using IRAS.E2ETests.Support;
using OpenQA.Selenium;

namespace IRAS.E2ETests;

public sealed class RoleFlowTests : E2ETestBase
{
    public RoleFlowTests(BrowserFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public void Employer_can_register_login_open_job_workflow_and_applicants()
    {
        if (SkipWhenNotConfigured())
        {
            return;
        }

        var stamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var email = $"e2e.employer.{stamp}@example.com";
        var password = "Password123!";

        RegisterEmployer(email, password, $"E2E Employer {stamp}");
        Login(email, password);

        Assert.True(BodyContains("Dashboard") || BodyContains("Employer") || BodyContains("Jobs"), DescribeCurrentPage());
        AssertNoServerError();

        OpenFirstAvailable("New job", "Create Job", "Post Job", "Job Postings", "Jobs");
        Assert.True(BodyContains("Job") || BodyContains("Title"), DescribeCurrentPage());

        TryFillJobForm(stamp);
        TryClickByText("Publish", "Create", "Save");
        AssertNoServerError();

        OpenFirstAvailable("Job Postings", "Jobs", "View all");
        Assert.True(BodyContains("Job") || BodyContains("Posting") || BodyContains("No"), DescribeCurrentPage());
        AssertNoServerError();
    }

    [Fact]
    public void Candidate_can_register_login_update_profile_and_open_application_workflows()
    {
        if (SkipWhenNotConfigured())
        {
            return;
        }

        var stamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var email = $"e2e.candidate.{stamp}@example.com";
        var password = "Password123!";

        RegisterCandidate(email, password, "E2E", $"Candidate{stamp}");
        Login(email, password);

        Assert.True(BodyContains("Dashboard") || BodyContains("Candidate") || BodyContains("Jobs"), DescribeCurrentPage());
        AssertNoServerError();

        OpenFirstAvailable("Profile");
        Assert.True(BodyContains("Profile") || BodyContains("Education") || BodyContains("Experience"), DescribeCurrentPage());
        TryClickByText("Save", "Update");
        AssertNoServerError();

        OpenFirstAvailable("Jobs", "Browse Jobs", "Find Jobs");
        Assert.True(BodyContains("Job") || BodyContains("Available"), DescribeCurrentPage());
        TryClickByText("Apply", "View", "Details");
        AssertNoServerError();

        OpenFirstAvailable("My Applications", "Applications");
        Assert.True(BodyContains("Application") || BodyContains("No"), DescribeCurrentPage());
        AssertNoServerError();

        OpenFirstAvailable("Skill Gaps", "Skill Gap");
        Assert.True(BodyContains("Skill") || BodyContains("Gap") || BodyContains("No"), DescribeCurrentPage());
        AssertNoServerError();

        OpenFirstAvailable("Interviews");
        Assert.True(BodyContains("Interview") || BodyContains("No"), DescribeCurrentPage());
        AssertNoServerError();
    }

    private void RegisterEmployer(string email, string password, string companyName)
    {
        OpenRegisterPage();
        TryClickByText("Employer");
        TypeIntoField(email, "email");
        TypeIntoField(password, "password");
        TryType("confirm", password);
        TryType("company", companyName);
        TryType("name", companyName);
        SubmitCurrentForm();
        WaitUntilRegisteredOrSignedIn();
    }

    private void RegisterCandidate(string email, string password, string firstName, string lastName)
    {
        OpenRegisterPage();
        TryClickByText("Candidate");
        TryType("first", firstName);
        TryType("last", lastName);
        TypeIntoField(email, "email");
        TypeIntoField(password, "password");
        TryType("confirm", password);
        SubmitCurrentForm();
        WaitUntilRegisteredOrSignedIn();
    }

    private void OpenRegisterPage()
    {
        ClearBrowserState();
        GoTo("/register");

        if (!BodyContains("Create") && !BodyContains("Register") && !BodyContains("account"))
        {
            GoTo("/login");
            ClickByText("Create one", "Create account", "Register", "Sign up");
        }
    }

    private void WaitUntilRegisteredOrSignedIn()
    {
        var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(30));
        wait.Until(_ => !Driver.Url.Contains("/register", StringComparison.OrdinalIgnoreCase)
            || BodyContains("Dashboard")
            || BodyContains("Sign out")
            || BodyContains("already exists")
            || BodyContains("Invalid"));

        Assert.DoesNotContain("Invalid", BodyText, StringComparison.OrdinalIgnoreCase);
        AssertNoServerError();
    }

    private void OpenFirstAvailable(params string[] labels)
    {
        foreach (var label in labels)
        {
            if (TryClickByText(label))
            {
                return;
            }
        }

        throw new NoSuchElementException($"Could not open any of: {string.Join(", ", labels)}.{Environment.NewLine}{DescribeCurrentPage()}");
    }

    private void TryFillJobForm(long stamp)
    {
        TryType("title", $"E2E QA Engineer {stamp}");
        TryType("location", "Colombo");
        TryType("experience", "1");
        TryType("requirement", "Selenium, C#, API testing");
        TryType("description", "Automated E2E testing role created by Selenium.");
        TryType("skill", "Selenium");
        TryClickByText("Add Skill", "Add", "Generate");
    }

    private bool TryType(string hint, string value)
    {
        try
        {
            TypeIntoField(value, hint);
            return true;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }
}
