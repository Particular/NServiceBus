#nullable enable

namespace NServiceBus;

using System;

class UsageReporterSettings
{
    public required string EndpointName { get; init; }
    public required string BaseQueueAddress { get; init; }
    public required TimeSpan ReportingInterval { get; init; }
}
