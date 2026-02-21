using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Linq;
using Xunit;

namespace TestProject.SeleniumTests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    public string ServerAddress { get; private set; }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = builder.Build();
        host.Start();

        // Get the address the server is listening on
        var server = host.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>();
        var addresses = server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
        ServerAddress = addresses.Addresses.FirstOrDefault() ?? "http://localhost:5000";

        return host;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseKestrel(options =>
        {
            options.ListenAnyIP(0); // Listen on a random port
        });
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
        // Act
        // This will ensure the server is started
        var client = _factory.CreateClient(); 
        
        _driver.Navigate().GoToUrl(_factory.ServerAddress);

        // Assert
        Assert.Contains("Welcome", _driver.PageSource);
    }

    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
    }
}
