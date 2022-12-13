namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Pipeline;
using Recoverability.Settings;

class HttpRateLimitBehavior : Behavior<IRecoverabilityContext>
{
    const string RetryAfterMsHeaderName = "retry-after-ms";
    const string XRetryAfterMsHeaderName = "x-ms-retry-after-ms";

    public HttpRateLimitBehavior(int maxAmountOfDelayedRetriesForThrottling, List<IHttpRateLimitStrategy> throttlingStrategies)
    {
        this.maxAmountOfDelayedRetriesForThrottling = maxAmountOfDelayedRetriesForThrottling;
        this.throttlingStrategies = throttlingStrategies;
    }

    public override Task Invoke(IRecoverabilityContext context, Func<Task> next)
    {
        // avoid infinite retries
        if (context.DelayedDeliveriesPerformed >= maxAmountOfDelayedRetriesForThrottling)
        {
            context.RecoverabilityAction = RecoverabilityAction.MoveToError(context.RecoverabilityConfiguration.Failed.ErrorQueue);
            return next();
        }

        // we only handle HttpRequestException
        if (context.Exception is not HttpRequestException httpException)
        {
            return next();
        }

        // Get the Http status code value, either from the exception data or the exception
        int? statusCode = null;

#if NET6_0_OR_GREATER
        statusCode = (int)httpException.StatusCode;
#else
        statusCode = (int?)httpException.Data["NServiceBus.HttpError.StatusCode"];
#endif

        if (!statusCode.HasValue)
        {
            return next();
        }

        var headers = httpException.Data["NServiceBus.HttpError.HttpResponseHeaders"];
        // if we don't have any headers, apply the default delay strategy since we can't make decisions
        if (headers is not HttpResponseHeaders httpResponseHeaders)
        {
            // todo: set the default delayed retry time increase
            context.RecoverabilityAction = RecoverabilityAction.DelayedRetry(TimeSpan.Zero);
            return next();
        }

        // invoke user strategies
        var strategy = throttlingStrategies.SingleOrDefault(x => (int)x.StatusCode == statusCode);
        var delay = strategy?.GetDelay(httpException, statusCode.Value, httpResponseHeaders);
        if (delay != null)
        {
            context.RecoverabilityAction = RecoverabilityAction.DelayedRetry(delay.Value);
            return next();
        }

        // if it's not 429 or 503, don't do anything
#pragma warning disable IDE0078
        if (statusCode != 429 && statusCode != 503)
#pragma warning restore IDE0078
        {
            return next();
        }

        // handle known headers
        if (httpResponseHeaders.RetryAfter != null)
        {
            if (httpResponseHeaders.RetryAfter.Delta.HasValue)
            {
                context.RecoverabilityAction =
                    RecoverabilityAction.DelayedRetry(
                        httpResponseHeaders.RetryAfter.Delta.GetValueOrDefault());
                return next();
            }

            if (httpResponseHeaders.RetryAfter.Date.HasValue)
            {
                var delta = httpResponseHeaders.RetryAfter.Date.Value.Subtract(DateTimeOffset.UtcNow);
                context.RecoverabilityAction = RecoverabilityAction.DelayedRetry(delta);
                return next();
            }
        }

        httpResponseHeaders.TryGetValues(RetryAfterMsHeaderName, out var retryAfterMsHeaderValues);
        var headerValue = retryAfterMsHeaderValues?.FirstOrDefault();
        if (headerValue != null)
        {
            if (int.TryParse(headerValue, out int serverDelayInMilliseconds))
            {
                context.RecoverabilityAction =
                    RecoverabilityAction.DelayedRetry(TimeSpan.FromMilliseconds(serverDelayInMilliseconds));
                return next();
            }
        }

        httpResponseHeaders.TryGetValues(XRetryAfterMsHeaderName, out var xRetryAfterMsHeaderValues);
        headerValue = xRetryAfterMsHeaderValues?.FirstOrDefault();
        if (headerValue != null)
        {
            if (int.TryParse(headerValue, out int serverDelayInMilliseconds))
            {
                context.RecoverabilityAction =
                    RecoverabilityAction.DelayedRetry(TimeSpan.FromMilliseconds(serverDelayInMilliseconds));
                return next();
            }
        }

        // exhausted all options, defer.
        return next();
    }

    int maxAmountOfDelayedRetriesForThrottling;
    readonly List<IHttpRateLimitStrategy> throttlingStrategies;
}