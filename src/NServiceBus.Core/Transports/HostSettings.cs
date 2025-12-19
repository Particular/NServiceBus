#nullable enable

namespace NServiceBus.Transport;

using System;
using System.Diagnostics.CodeAnalysis;
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
    /// Indicates whether the transport is running in an NServiceBus endpoint.
    /// </summary>
    /// <remarks>
    /// This property returns <c>true</c> if the transport is running in an NServiceBus endpoint; otherwise, <c>false</c> indicating the transports is running in the raw mode.</remarks>
    [MemberNotNullWhen(true, nameof(CoreSettings), nameof(ServiceProvider))]
    public bool IsHosted => CoreSettings is not null && ServiceProvider is not null;

    /// <summary>
    /// Indicates whether the transport supports dependency injection.
    /// </summary>
    /// <remarks>
    /// This property returns <c>true</c> if the associated service provider is initialized; otherwise, <c>false</c>. When the endpoint is running in an NServiceBus endpoint, this property will always return <c>true</c>.
    /// In raw mode, this property will return <c>false</c> unless the hosting infrastructure has initialized the service provider and assigned it to the <see cref="ServiceProvider"/> property before the transport definition is initialized.
    /// Transport implementations can use this property to determine whether they can resolve dependencies from the service provider during initialization.
    /// </remarks>
    [MemberNotNullWhen(true, nameof(ServiceProvider))]
    public bool SupportsDependencyInjection => ServiceProvider is not null;

    /// <summary>
    /// Settings available only when running hosted in an NServiceBus endpoint; Otherwise, <c>null</c>.
    /// Transports can use these settings to validate the hosting endpoint settings.
    /// </summary>
    public IReadOnlySettings? CoreSettings { get; } = coreSettings;

    /// <summary>
    /// Service provider available only when running hosted in an NServiceBus endpoint; Otherwise, <c>null</c>.
    /// Transports can use the provider in hosted scenarios to resolve dependencies from the provider.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; set; }

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
}
