namespace NServiceBus.Recoverability.Settings;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

/// <summary>
/// A contract that allows for customization of the delayed retry strategy based on the <see cref="HttpRequestException"/> and <see cref="HttpResponseHeaders"/>.
/// </summary>
public interface IHttpRateLimitStrategy
{
    /// <summary>
    /// Status code returned by the HTTP API
    /// </summary>
    HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the delay to apply to the next retry.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="statusCode">The http status code returned by the API.</param>
    /// <param name="headers">The response headers.</param>
    /// <returns></returns>
    TimeSpan? GetDelay(HttpRequestException exception, int statusCode, HttpResponseHeaders headers);
}