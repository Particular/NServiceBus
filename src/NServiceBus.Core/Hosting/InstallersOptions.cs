#nullable enable

namespace NServiceBus;

/// <summary>
/// Options for controlling installer behavior when using <see cref="ServiceCollectionExtensions.AddNServiceBusInstallers"/>.
/// </summary>
public sealed class InstallersOptions
{
    internal bool Enabled { get; init; }

    /// <summary>
    /// Controls how the host behaves after installers have completed.
    /// </summary>
    public InstallersShutdownBehavior ShutdownBehavior { get; set; } = InstallersShutdownBehavior.StopApplication;
}