﻿using Microsoft.Extensions.Options;
using Recaptcha.Verify.Net.Configuration;
using Recaptcha.Verify.Net.Exceptions;
using Recaptcha.Verify.Net.Logging;
using Refit;
using System.Threading;
using System.Threading.Tasks;

namespace Recaptcha.Verify.Net
{
    /// <inheritdoc />
    public class RecaptchaService : IRecaptchaService
    {
        private readonly RecaptchaOptions _recaptchaOptions;
        private readonly IRecaptchaClient _recaptchaClient;
        private readonly IRecaptchaLoggerService _logger;

        /// <summary>
        /// Recaptcha service constructor.
        /// </summary>
        /// <param name="recaptchaOptions">Recaptcha options.</param>
        /// <param name="recaptchaClient">Recaptcha Refit client.</param>
        public RecaptchaService(IOptions<RecaptchaOptions> recaptchaOptions, IRecaptchaClient recaptchaClient, IRecaptchaLoggerService recaptchaLoggerService)
        {
            _recaptchaOptions = recaptchaOptions?.Value;
            _recaptchaClient = recaptchaClient;
            _logger = recaptchaLoggerService;
        }

        /// <inheritdoc />
        public Task<CheckResult> VerifyAndCheckAsync(string response, CancellationToken cancellationToken = default) =>
            VerifyAndCheckCoreAsync(new VerifyRequest() { Response = response }, null, null, cancellationToken);

        /// <inheritdoc />
        public Task<CheckResult> VerifyAndCheckAsync(string response, string action, CancellationToken cancellationToken = default) =>
            VerifyAndCheckCoreAsync(new VerifyRequest() { Response = response }, action, null, cancellationToken);

        /// <inheritdoc />
        public Task<CheckResult> VerifyAndCheckAsync(string response, string action, float score, CancellationToken cancellationToken = default) =>
            VerifyAndCheckCoreAsync(new VerifyRequest() { Response = response }, action, score, cancellationToken);

        /// <inheritdoc />
        public Task<CheckResult> VerifyAndCheckAsync(VerifyRequest request, CancellationToken cancellationToken = default) =>
            VerifyAndCheckCoreAsync(request, null, null, cancellationToken);

        /// <inheritdoc />
        public Task<CheckResult> VerifyAndCheckAsync(VerifyRequest request, string action, CancellationToken cancellationToken = default) =>
            VerifyAndCheckCoreAsync(request, action, null, cancellationToken);

        /// <inheritdoc />
        public Task<CheckResult> VerifyAndCheckAsync(VerifyRequest request, string action, float score, CancellationToken cancellationToken = default) =>
            VerifyAndCheckCoreAsync(request, action, score, cancellationToken);

        /// <inheritdoc />
        public Task<VerifyResponse> VerifyAsync(string response, string secret = null, string remoteIp = null, CancellationToken cancellationToken = default) =>
            VerifyAsync(
                new VerifyRequest()
                {
                    Response = response,
                    Secret = secret,
                    RemoteIp = remoteIp
                },
                cancellationToken);

        /// <inheritdoc />
        public async Task<VerifyResponse> VerifyAsync(VerifyRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Secret))
            {
                if (string.IsNullOrWhiteSpace(_recaptchaOptions?.SecretKey))
                {
                    var e = new SecretKeyNotSpecifiedException();
                    _logger.Log(RecaptchaServiceEventId.ServiceException, e);
                    throw e;
                }

                request.Secret = _recaptchaOptions.SecretKey;
            }

            if (string.IsNullOrWhiteSpace(request.Response))
            {
                var e = new EmptyCaptchaAnswerException();
                _logger.Log(RecaptchaServiceEventId.ServiceException, e);
                throw e;
            }

            try
            {
                _logger.Log(RecaptchaServiceEventId.SendingRequest, request.Response, request.RemoteIp);
                var result = await _recaptchaClient.VerifyAsync(request, cancellationToken);
                _logger.Log(RecaptchaServiceEventId.RequestSucceded, result);
                return result;
            }
            catch (ApiException e)
            {
                var se = new HttpRequestException(e);
                _logger.Log(RecaptchaServiceEventId.ServiceException, se);
                throw se;
            }
        }

        private async Task<CheckResult> VerifyAndCheckCoreAsync(VerifyRequest request, string action, float? score, CancellationToken cancellationToken)
        {
            var response = await VerifyAsync(request, cancellationToken);

            var checkResult = new CheckResult()
            {
                Response = response,
                ActionMatches = false,
                ScoreSatisfies = false,
            };

            if (response.Success && response.IsV3)
            {
                string actionToCheck;
                if (!string.IsNullOrWhiteSpace(action))
                {
                    actionToCheck = action;
                }
                else if (!string.IsNullOrWhiteSpace(_recaptchaOptions?.Action))
                {
                    actionToCheck = _recaptchaOptions.Action;
                }
                else
                {
                    var e = new EmptyActionException();
                    _logger.Log(RecaptchaServiceEventId.ServiceException, e);
                    throw e;
                }

                checkResult.ActionMatches = response.Success && actionToCheck.Equals(response.Action);

                float scoreThreshold;
                if (score.HasValue)
                {
                    scoreThreshold = score.Value;
                }
                else if (_recaptchaOptions?.ActionsScoreThresholds != null && _recaptchaOptions.ActionsScoreThresholds.TryGetValue(action, out scoreThreshold))
                {
                }
                else if (_recaptchaOptions != null && _recaptchaOptions.ScoreThreshold.HasValue)
                {
                    scoreThreshold = _recaptchaOptions.ScoreThreshold.Value;
                }
                else
                {
                    var e = new MinScoreNotSpecifiedException(actionToCheck);
                    _logger.Log(RecaptchaServiceEventId.ServiceException, e);
                    throw e;
                }

                checkResult.ScoreSatisfies = response.Score.Value >= scoreThreshold;
            }

            _logger.Log(RecaptchaServiceEventId.VerifyResponseChecked, checkResult);
            return checkResult;
        }
    }
}
