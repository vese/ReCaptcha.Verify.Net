# Recaptcha.Verify.Net
[![NuGet](https://img.shields.io/nuget/v/Recaptcha.Verify.Net.svg)](https://www.nuget.org/packages/Recaptcha.Verify.Net)

Library for verifying Google reCAPTCHA v2/v3 response token for ASP.NET Core. The project targets .NET Core 3.1.

### Installation
Package can be installed using Visual Studio UI (Tools > NuGet Package Manager > Manage NuGet Packages for Solution and search for "Recaptcha.Verify.Net").

Also latest version of package can be installed using Package Manager Console:
```
PM> Install-Package Recaptcha.Verify.Net
```

### Using reCAPTCHA verification
1. Add secret key in appsettings.json file
```json
{
  "Recaptcha": {
    "SecretKey": "<recaptcha secret key>",
    "ScoreThreshold": 0.5
  }
}
```
2. Configure service in Startup.cs
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.ConfigureRecaptcha(Configuration.GetSection("Recaptcha"));
    //...
}
```
3. Use service in controller to verify captcha answer
```csharp
[ApiController]
[Route("api/[controller]")]
public class LoginController : Controller
{
    private readonly ILogger _logger;
    private readonly IRecaptchaService _recaptchaService;

    public LoginController(ILoggerFactory loggerFactory, IRecaptchaService recaptchaService)
    {
        _logger = loggerFactory.CreateLogger<LoginController>();
        _recaptchaService = recaptchaService;
    }

    [HttpPost]
    public async Task<IActionResult> Login([FromBody] Credentials credentials, CancellationToken cancellationToken)
    {
        var checkResult = await _recaptchaService.VerifyAndCheckAsync(
            credentials.RecaptchaToken,
            credentials.Action,
            cancellationToken);
        
        if (!checkResult.Success)
        {
            if (!checkResult.ScoreSatisfies)
            {
                // Handle score less than specified threshold for v3
                return BadRequest();
            }
        
            if (!checkResult.Response.Success)
            {
                _logger.LogError($"Recaptcha error: {JsonConvert.SerializeObject(checkResult.Response.ErrorCodes)}");
            }
            return BadRequest();
        }
        
        // Process login
        
        return Ok();
    }
}
```
### Examples
Examples could be found in library repository:
- [**Recaptcha.Verify.Net.ConsoleApp**](https://github.com/vese/Recaptcha.Verify.Net/blob/master/examples/Recaptcha.Verify.Net.ConsoleApp/Program.cs "Link") (.NET Core 3.1)
- [**Recaptcha.Verify.Net.AspNetCoreAngular**](https://github.com/vese/Recaptcha.Verify.Net/blob/master/examples/Recaptcha.Verify.Net.AspNetCoreAngular/Controllers/LoginController.cs "Link") (ASP.NET Core 3.1 + Angular)
