#nullable enable

namespace NServiceBus.Transport;

using System;
using System.Threading;
using Settings;

/// <summary>
/// Contains information about the hosting environment that is using the transport.
/// </summary>
/// <remarks>
/// Creates a new instance of <see cref="HostSettings"/>.
/// </remarks>
public class HostSettings(string name, string hostDisplayName, StartupDiagnosticEntries startupDiagnostic, Action<string, Exception, CancellationToken> criticalErrorAction, bool setupInfrastructure, IReadOnlySettings? coreSettings = null)
{
    /// <summary>
    /// Settings available only when running hosted in an NServiceBus endpoint; Otherwise, <c>null</c>.
    /// Transports can use these settings to validate the hosting endpoint settings.
    /// </summary>
    public IReadOnlySettings? CoreSettings { get; } = coreSettings;

    /// <summary>
    /// A name that describes the host (e.g. the endpoint name).
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// The name for the host as it should be shown on UIs.
    /// </summary>
    public string HostDisplayName { get; } = hostDisplayName;

    /// <summary>
    /// A <see cref="StartupDiagnosticEntries"/> instance that can store diagnostic information about this transport.
    /// </summary>
    public StartupDiagnosticEntries StartupDiagnostic { get; } = startupDiagnostic;

    /// <summary>
    /// A callback to invoke when exception occur that can't be handled by the transport.
    /// </summary>
    public Action<string, Exception, CancellationToken> CriticalErrorAction { get; } = criticalErrorAction;

    /// <summary>
    /// A flag that indicates whether the transport should automatically setup necessary infrastructure.
    /// </summary>
    public bool SetupInfrastructure { get; } = setupInfrastructure;

    /// <summary>
    /// The service provider of the NServiceBus endpoint using the transport.
    /// NOTE: When running outside an endpoint this will be null.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; internal set; }
}