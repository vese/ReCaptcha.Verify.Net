﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Recaptcha.Verify.Net.AspNetCoreAngular.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Recaptcha.Verify.Net.AspNetCoreAngular.Controllers
{
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
}
