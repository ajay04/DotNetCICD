using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace TestProject.SeleniumTests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseKestrel(options =>
        {
            options.ListenAnyIP(5005);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = builder.Build();
        host.Start();
        return host;
    }
}

public class HomePageTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly IWebDriver _driver;
    private readonly string _baseUrl = "http://localhost:5005";

    public HomePageTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;

        var options = new ChromeOptions();
        options.AddArgument("--headless");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");

        _driver = new ChromeDriver(options);
    }

    [Fact]
    public void HomePage_Should_Load_Successfully()
    {
        // This ensures the server is started
        using var client = _factory.CreateClient();
        
        _driver.Navigate().GoToUrl(_baseUrl);
        Assert.Contains("Welcome", _driver.PageSource);
    }

    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
    }
}
