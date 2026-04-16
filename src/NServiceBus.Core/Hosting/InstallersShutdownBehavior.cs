#nullable enable

namespace NServiceBus;

/// <summary>
/// Controls how the host behaves after installers have completed.
/// </summary>
public enum InstallersShutdownBehavior
{
    /// <summary>
    /// Calls <see cref="Microsoft.Extensions.Hosting.IHostApplicationLifetime.StopApplication"/> after
    /// installers complete, which triggers graceful host shutdown.
    /// </summary>
    StopApplication,

    /// <summary>
    /// No automatic shutdown; the application continues running after installers complete.
    /// </summary>
    Continue
}