#nullable enable

namespace NServiceBus;

using System;

class EndpointUsageReport
{
    public required string EndpointName { get; set; }
    public DateTimeOffset TimeStamp { get; set; }
    public long MessagesSuccessfullyProcessed { get; set; }
}
