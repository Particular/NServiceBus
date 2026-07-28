#nullable enable

namespace NServiceBus;

using System;
using System.Runtime;

sealed class HostingDiagnostics
{
    public required Guid HostId { get; init; }
    public required string HostDisplayName { get; init; }
    public required string MachineName { get; init; }
    public required PlatformID OSPlatform { get; init; }
    public required string OSVersion { get; init; }
    public required bool IsServerGC { get; init; }
    public required GCLatencyMode GCLatencyMode { get; init; }
    public required int ProcessorCount { get; init; }
    public required bool Is64BitProcess { get; init; }
    public required Version CLRVersion { get; init; }
    public required long WorkingSet { get; init; }
    public required int SystemPageSize { get; init; }
    public required string HostName { get; init; }
    public required string UserName { get; init; }
    public required string PathToExe { get; init; }
    public required bool InstallersEnabled { get; init; }
}
