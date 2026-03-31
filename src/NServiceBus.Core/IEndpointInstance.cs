#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Particular.Obsoletes;

/// <summary>
/// Represents an endpoint in the running phase.
/// </summary>
[ObsoleteMetadata(
    Message = "Self-hosting an endpoint is no longer recommended. Instead, consider using a Microsoft IHostApplicationBuilder-based host to manage the lifecycle of your endpoint",
    TreatAsErrorFromVersion = "11",
    RemoveInVersion = "12",
    ReplacementTypeOrMember = "IServiceCollection.AddNServiceBusEndpoint")]
[Obsolete("Self-hosting an endpoint is no longer recommended. Instead, consider using a Microsoft IHostApplicationBuilder-based host to manage the lifecycle of your endpoint. Use 'IServiceCollection.AddNServiceBusEndpoint' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
public interface IEndpointInstance : IMessageSession
{
    /// <summary>
    /// Stops the endpoint.
    /// </summary>
    [ObsoleteMetadata(
        Message = "Self-hosting an endpoint is no longer recommended. Instead, consider using a Microsoft IHostApplicationBuilder-based host to manage the lifecycle of your endpoint",
        TreatAsErrorFromVersion = "11",
        RemoveInVersion = "12",
        ReplacementTypeOrMember = "IServiceCollection.AddNServiceBusEndpoint")]
    [Obsolete("Self-hosting an endpoint is no longer recommended. Instead, consider using a Microsoft IHostApplicationBuilder-based host to manage the lifecycle of your endpoint. Use 'IServiceCollection.AddNServiceBusEndpoint' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
    Task Stop(CancellationToken cancellationToken = default);
}