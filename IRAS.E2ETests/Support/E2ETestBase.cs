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

    protected IWebElement WaitForAnyInput(Func<IWebElement, bool> predicate, int seconds = 10)
    {
        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(seconds));
        return wait.Until(driver =>
        {
            return driver.FindElements(By.TagName("input"))
                .FirstOrDefault(input => input.Displayed && predicate(input));
        });
    }
}
