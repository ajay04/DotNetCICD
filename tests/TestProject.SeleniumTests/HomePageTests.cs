using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Linq;
using Xunit;
using System.Net;

namespace TestProject.SeleniumTests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private IHost? _host;
    public string ServerAddress { get; private set; } = "http://localhost:5005"; // Default fallback

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Use a random port
        builder.ConfigureWebHost(webBuilder =>
        {
            webBuilder.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        });

        _host = builder.Build();
        _host.Start();

        var server = _host.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        ServerAddress = addresses?.Addresses.FirstOrDefault() ?? "http://localhost:5005";

        return _host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _host?.Dispose();
        }
    }
}

public class HomePageTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly IWebDriver _driver;

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
        // Ensure the server is started
        _factory.CreateDefaultClient(); 
        
        _driver.Navigate().GoToUrl(_factory.ServerAddress);
        Assert.Contains("Welcome", _driver.PageSource);
    }

    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
    }
}
