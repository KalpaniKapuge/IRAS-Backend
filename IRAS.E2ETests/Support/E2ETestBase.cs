using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Text.RegularExpressions;

namespace IRAS.E2ETests.Support;

public abstract class E2ETestBase : IClassFixture<BrowserFixture>
{
    private readonly BrowserFixture _fixture;

    protected E2ETestBase(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    protected IWebDriver Driver => _fixture.Driver;

    protected E2ETestSettings Settings => _fixture.Settings;

    protected bool SkipWhenNotConfigured()
    {
        if (Settings.IsConfigured)
        {
            return false;
        }

        Console.WriteLine("Skipped: set IRAS_E2E_BASE_URL to run Selenium E2E tests.");
        return true;
    }

    protected void GoTo(string path)
    {
        Driver.Navigate().GoToUrl(Settings.BuildUri(path));
        WaitForPageReady();
    }

    protected void ClearBrowserState()
    {
        if (!Settings.IsConfigured)
        {
            return;
        }

        Driver.Navigate().GoToUrl(Settings.BuildUri("/"));
        WaitForPageReady();
        Driver.Manage().Cookies.DeleteAllCookies();

        if (Driver is IJavaScriptExecutor js)
        {
            js.ExecuteScript("window.localStorage.clear(); window.sessionStorage.clear();");
        }
    }

    protected IWebElement WaitFor(By selector, int seconds = 10)
    {
        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(seconds));
        return wait.Until(driver =>
        {
            var element = driver.FindElement(selector);
            return element.Displayed ? element : null;
        });
    }

    protected string BodyText => Driver.FindElements(By.TagName("body")).FirstOrDefault()?.Text ?? string.Empty;

    protected bool BodyContains(string text) =>
        BodyText.Contains(text, StringComparison.OrdinalIgnoreCase);

    protected void WaitForBodyContains(string text, int seconds = 30)
    {
        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(seconds));
        wait.Until(_ => BodyContains(text));
    }

    protected void ClickByText(params string[] texts)
    {
        foreach (var text in texts)
        {
            var element = FindClickableByText(text);
            if (element is null)
            {
                continue;
            }

            ClickElement(element);
            WaitForPageReady();
            return;
        }

        throw new NoSuchElementException($"Could not find clickable text: {string.Join(", ", texts)}.{Environment.NewLine}{DescribeCurrentPage()}");
    }

    protected bool TryClickByText(params string[] texts)
    {
        foreach (var text in texts)
        {
            var element = FindClickableByText(text);
            if (element is null)
            {
                continue;
            }

            ClickElement(element);
            WaitForPageReady();
            return true;
        }

        return false;
    }

    protected void TypeIntoField(string value, params string[] hints)
    {
        var input = FindInputByHints(hints)
            ?? throw new NoSuchElementException($"Could not find input for: {string.Join(", ", hints)}.{Environment.NewLine}{DescribeCurrentPage()}");

        input.Clear();
        input.SendKeys(value);
    }

    protected void TypeIntoFirstEmptyInput(string value)
    {
        var input = Driver.FindElements(By.TagName("input"))
            .FirstOrDefault(i => i.Displayed && i.Enabled && string.IsNullOrWhiteSpace(i.GetAttribute("value")));

        if (input is null)
        {
            throw new NoSuchElementException($"Could not find an empty input.{Environment.NewLine}{DescribeCurrentPage()}");
        }

        input.Clear();
        input.SendKeys(value);
    }

    protected void SubmitCurrentForm()
    {
        var submit = Driver.FindElements(By.CssSelector("button[type='submit'], input[type='submit']"))
            .FirstOrDefault(e => e.Displayed && e.Enabled);

        if (submit is not null)
        {
            ClickElement(submit);
            WaitForPageReady();
            return;
        }

        ClickByText("Sign in", "Log in", "Login", "Create account", "Register", "Submit", "Save", "Publish", "Create");
    }

    protected void Login(string email, string password)
    {
        ClearBrowserState();
        GoTo("/login");

        TypeIntoField(email, "email");
        TypeIntoField(password, "password");
        SubmitCurrentForm();

        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(30));
        wait.Until(_ => !Driver.Url.Contains("/login", StringComparison.OrdinalIgnoreCase)
            || Driver.Url.Contains("/admin", StringComparison.OrdinalIgnoreCase)
            || Driver.Url.Contains("/candidate", StringComparison.OrdinalIgnoreCase)
            || Driver.Url.Contains("/employer", StringComparison.OrdinalIgnoreCase)
            || BodyContains("Invalid")
            || BodyContains("Sign out"));

        Assert.DoesNotContain("Invalid", BodyText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/login", Driver.Url, StringComparison.OrdinalIgnoreCase);
    }

    protected void AssertNoServerError()
    {
        var body = BodyText;
        Assert.False(Regex.IsMatch(body, @"\b404\b|not found", RegexOptions.IgnoreCase), DescribeCurrentPage());
        Assert.False(Regex.IsMatch(body, @"\b500\b|internal server error", RegexOptions.IgnoreCase), DescribeCurrentPage());
        Assert.DoesNotContain("application error", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Something went wrong", body, StringComparison.OrdinalIgnoreCase);
    }

    protected IWebElement WaitForAnyInput(Func<IWebElement, bool> predicate, int seconds = 30)
    {
        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(seconds));
        try
        {
            return wait.Until(driver =>
            {
                return driver.FindElements(By.TagName("input"))
                    .FirstOrDefault(input => input.Displayed && predicate(input));
            });
        }
        catch (WebDriverTimeoutException ex)
        {
            throw new WebDriverTimeoutException(
                $"{ex.Message}{Environment.NewLine}{DescribeCurrentPage()}",
                ex);
        }
    }

    protected void WaitForPageReady()
    {
        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(30));
        wait.Until(driver =>
        {
            if (driver is not IJavaScriptExecutor js)
            {
                return true;
            }

            return string.Equals(js.ExecuteScript("return document.readyState")?.ToString(), "complete", StringComparison.OrdinalIgnoreCase);
        });
    }

    protected string DescribeCurrentPage()
    {
        var body = Driver.FindElements(By.TagName("body")).FirstOrDefault()?.Text ?? string.Empty;
        if (body.Length > 500)
        {
            body = body[..500];
        }

        return $"Current URL: {Driver.Url}{Environment.NewLine}Title: {Driver.Title}{Environment.NewLine}Body preview: {body}";
    }

    private void ClickElement(IWebElement element)
    {
        try
        {
            if (Driver is IJavaScriptExecutor js)
            {
                js.ExecuteScript("arguments[0].scrollIntoView({block:'center', inline:'center'});", element);
            }

            element.Click();
        }
        catch (ElementClickInterceptedException)
        {
            if (Driver is not IJavaScriptExecutor js)
            {
                throw;
            }

            js.ExecuteScript("arguments[0].click();", element);
        }
    }

    private IWebElement? FindClickableByText(string text)
    {
        var xpathText = XPathLiteral(text.ToLowerInvariant());
        var candidates = Driver.FindElements(By.XPath(
            $"//*[self::a or self::button or @role='button'][contains(translate(normalize-space(.), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), {xpathText})]"))
            .Concat(Driver.FindElements(By.XPath(
                $"//*[contains(translate(normalize-space(.), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), {xpathText})]/ancestor-or-self::*[self::a or self::button or @role='button'][1]")));

        return candidates.FirstOrDefault(e => e.Displayed && e.Enabled);
    }

    private IWebElement? FindInputByHints(IEnumerable<string> hints)
    {
        var visibleInputs = Driver.FindElements(By.CssSelector("input, textarea, select"))
            .Where(i => i.Displayed && i.Enabled)
            .ToList();

        foreach (var hint in hints)
        {
            var normalized = hint.Trim();
            var direct = visibleInputs.FirstOrDefault(input =>
                Contains(input, "type", normalized)
                || Contains(input, "name", normalized)
                || Contains(input, "id", normalized)
                || Contains(input, "autocomplete", normalized)
                || Contains(input, "placeholder", normalized)
                || LabelFor(input).Contains(normalized, StringComparison.OrdinalIgnoreCase));

            if (direct is not null)
            {
                return direct;
            }
        }

        return null;
    }

    private string LabelFor(IWebElement input)
    {
        var id = input.GetAttribute("id");
        if (!string.IsNullOrWhiteSpace(id))
        {
            var label = Driver.FindElements(By.CssSelector($"label[for='{id}']")).FirstOrDefault();
            if (label is not null)
            {
                return label.Text;
            }
        }

        return input.FindElements(By.XPath("./ancestor::label[1]")).FirstOrDefault()?.Text ?? string.Empty;
    }

    private static bool Contains(IWebElement element, string attribute, string expected) =>
        element.GetAttribute(attribute)?.Contains(expected, StringComparison.OrdinalIgnoreCase) == true;

    private static string XPathLiteral(string value)
    {
        if (!value.Contains('\''))
        {
            return $"'{value}'";
        }

        return $"\"{value}\"";
    }
}
