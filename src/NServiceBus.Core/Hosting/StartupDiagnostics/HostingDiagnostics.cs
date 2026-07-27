#nullable enable

namespace NServiceBus;

sealed class HostingDiagnostics
{
    public required string HostId { get; init; }
    public required string HostDisplayName { get; init; }
    public required string MachineName { get; init; }
    public required string OSPlatform { get; init; }
    public required string OSVersion { get; init; }
    public required bool IsServerGC { get; init; }
    public required string GCLatencyMode { get; init; }
    public required int ProcessorCount { get; init; }
    public required bool Is64BitProcess { get; init; }
    public required string CLRVersion { get; init; }
    public required long WorkingSet { get; init; }
    public required int SystemPageSize { get; init; }
    public required string HostName { get; init; }
    public required string UserName { get; init; }
    public required string PathToExe { get; init; }
    public required bool InstallersEnabled { get; init; }
}
