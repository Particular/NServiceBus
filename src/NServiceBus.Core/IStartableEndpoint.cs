#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Particular.Obsoletes;

/// <summary>
/// Represents an endpoint in the start-up phase.
/// </summary>
[ObsoleteMetadata(
    Message = "Self-hosting an endpoint is not recommended. Instead, consider using a hosting framework such as Generic Host or Web Host to manage the lifecycle including the start of your endpoint.",
    TreatAsErrorFromVersion = "11",
    RemoveInVersion = "12",
    ReplacementTypeOrMember = "services.AddNServiceBusEndpoint")]
[Obsolete("Self-hosting an endpoint is not recommended. Instead, consider using a hosting framework such as Generic Host or Web Host to manage the lifecycle including the start of your endpoint.. Use 'services.AddNServiceBusEndpoint' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
public interface IStartableEndpoint
{
    /// <summary>
    /// Starts the endpoint and returns a reference to it.
    /// </summary>
    /// <returns>A reference to the endpoint.</returns>
    [ObsoleteMetadata(
        Message = "Starting an endpoint manually is not recommended. Instead, consider using a hosting framework such as Generic Host or Web Host to manage the lifecycle including the start of your endpoint.",
        TreatAsErrorFromVersion = "11",
        RemoveInVersion = "12",
        ReplacementTypeOrMember = "services.AddNServiceBusEndpoint")]
    [Obsolete("Starting an endpoint manually is not recommended. Instead, consider using a hosting framework such as Generic Host or Web Host to manage the lifecycle including the start of your endpoint.. Use 'services.AddNServiceBusEndpoint' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
    Task<IEndpointInstance> Start(CancellationToken cancellationToken = default);
}