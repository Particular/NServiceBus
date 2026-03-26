namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Particular.Obsoletes;

/// <summary>
/// Extensions for <see cref="IEndpointInstance"/>.
/// </summary>
[ObsoleteMetadata(
    Message = "Self-hosting an endpoint is no longer recommended. Instead, consider using a Microsoft IHostApplicationBuilder-based host to manage the lifecycle of your endpoint",
    TreatAsErrorFromVersion = "11",
    RemoveInVersion = "12",
    ReplacementTypeOrMember = "IServiceCollection.AddNServiceBusEndpoint")]
[Obsolete("Self-hosting an endpoint is no longer recommended. Instead, consider using a Microsoft IHostApplicationBuilder-based host to manage the lifecycle of your endpoint. Use 'IServiceCollection.AddNServiceBusEndpoint' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
public static class EndpointInstanceExtensions
{
    /// <summary>
    /// Stops the endpoint from processing new messages,
    /// granting a period of time to gracefully complete processing before forceful cancellation.
    /// </summary>
    /// <param name="endpoint">The endpoint to stop.</param>
    /// <param name="gracefulStopTimeout">The length of time granted to gracefully complete processing.</param>
    [ObsoleteMetadata(
        Message = "Self-hosting an endpoint is no longer recommended. Instead, consider using a Microsoft IHostApplicationBuilder-based host to manage the lifecycle of your endpoint",
        TreatAsErrorFromVersion = "11",
        RemoveInVersion = "12",
        ReplacementTypeOrMember = "IServiceCollection.AddNServiceBusEndpoint")]
    [SuppressMessage("Code", "PS0018:A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext", Justification = "Convenience method wrapping the CancellationToken overload.")]
    [Obsolete("Self-hosting an endpoint is no longer recommended. Instead, consider using a Microsoft IHostApplicationBuilder-based host to manage the lifecycle of your endpoint. Use 'IServiceCollection.AddNServiceBusEndpoint' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
    public static async Task Stop(this IEndpointInstance endpoint, TimeSpan gracefulStopTimeout)
    {
        using var cancellationTokenSource = new CancellationTokenSource(gracefulStopTimeout);
        await endpoint.Stop(cancellationTokenSource.Token).ConfigureAwait(false);
    }
}
