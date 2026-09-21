using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

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

    protected IWebElement WaitFor(By selector, int seconds = 10)
    {
        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(seconds));
        return wait.Until(driver =>
        {
            var element = driver.FindElement(selector);
            return element.Displayed ? element : null;
        });
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

    private void WaitForPageReady()
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

    private string DescribeCurrentPage()
    {
        var body = Driver.FindElements(By.TagName("body")).FirstOrDefault()?.Text ?? string.Empty;
        if (body.Length > 500)
        {
            body = body[..500];
        }

        return $"Current URL: {Driver.Url}{Environment.NewLine}Title: {Driver.Title}{Environment.NewLine}Body preview: {body}";
    }
}
