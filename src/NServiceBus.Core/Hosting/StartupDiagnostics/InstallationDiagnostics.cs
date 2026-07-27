#nullable enable

namespace NServiceBus;

sealed class InstallationDiagnostics
{
    public required string[] InstallersEnabled { get; init; }
}
