using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace IRAS.E2ETests.Support;

public sealed class BrowserFixture : IDisposable
{
    private readonly Lazy<IWebDriver> _driver;

    internal E2ETestSettings Settings { get; } = new();

    internal IWebDriver Driver => _driver.Value;

    public BrowserFixture()
    {
        _driver = new Lazy<IWebDriver>(CreateDriver);
    }

    public void Dispose()
    {
        if (_driver.IsValueCreated)
        {
            Driver.Quit();
            Driver.Dispose();
        }
    }

    private IWebDriver CreateDriver()
    {
        var options = new ChromeOptions();
        options.AddArgument("--window-size=1440,1000");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--no-sandbox");

        if (Settings.Headless)
        {
            options.AddArgument("--headless=new");
        }

        return new ChromeDriver(options);
    }
}
