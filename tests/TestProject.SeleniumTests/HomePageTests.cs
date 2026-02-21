using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;
using Xunit.Abstractions;

namespace TestProject.SeleniumTests;

public class HomePageTests : IAsyncLifetime
{
    private IWebDriver? _driver;
    private Process? _webProcess;
    private readonly string _baseUrl = "http://127.0.0.1:5006"; 
    private readonly ITestOutputHelper _output;

    public HomePageTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        // Find project root dynamically
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, "TestProject.sln")))
        {
            currentDir = currentDir.Parent;
        }

        if (currentDir == null)
        {
            throw new Exception("Could not find solution root directory.");
        }

        var projectPath = Path.Combine(currentDir.FullName, "src", "TestProject.Web");
        _output.WriteLine($"Starting web project at: {projectPath}");

        // Detect current configuration (Debug or Release)
#if DEBUG
        string config = "Debug";
#else
        string config = "Release";
#endif

        _webProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{projectPath}\" --urls \"{_baseUrl}\" -c {config} --no-launch-profile",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        _webProcess.OutputDataReceived += (s, e) => { if (e.Data != null) _output.WriteLine("WEB: " + e.Data); };
        _webProcess.ErrorDataReceived += (s, e) => { if (e.Data != null) _output.WriteLine("WEB ERR: " + e.Data); };

        _webProcess.Start();
        _webProcess.BeginOutputReadLine();
        _webProcess.BeginErrorReadLine();

        // Setup Selenium
        var options = new ChromeOptions();
        options.AddArgument("--headless");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");

        _driver = new ChromeDriver(options);

        // Poll the server until it's ready
        using var client = new HttpClient();
        var sw = Stopwatch.StartNew();
        bool isReady = false;
        while (sw.Elapsed < TimeSpan.FromSeconds(30))
        {
            try
            {
                var response = await client.GetAsync(_baseUrl);
                if (response.IsSuccessStatusCode)
                {
                    isReady = true;
                    break;
                }
            }
            catch { /* Ignore and retry */ }
            await Task.Delay(1000);
        }

        if (!isReady)
        {
            throw new Exception($"Web server failed to start at {_baseUrl} within 30 seconds.");
        }
    }

    [Fact]
    public void HomePage_Should_Load_Successfully()
    {
        _driver.Navigate().GoToUrl(_baseUrl);
        _output.WriteLine("Current URL: " + _driver.Url);
        Assert.Contains("Welcome", _driver.PageSource);
    }

    public Task DisposeAsync()
    {
        _driver?.Quit();
        _driver?.Dispose();
        
        try
        {
            if (_webProcess != null && !_webProcess.HasExited)
            {
                _webProcess.Kill(true);
            }
        }
        catch { }

        return Task.CompletedTask;
    }
}
