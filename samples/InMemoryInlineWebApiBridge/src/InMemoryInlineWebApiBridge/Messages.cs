using NServiceBus;

namespace InMemoryInlineWebApiBridge;

public sealed class RetryInlineCommand : ICommand
{
    public string CorrelationId { get; set; } = string.Empty;
    public int FailuresBeforeSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class AlwaysFailInlineCommand : ICommand
{
    public string CorrelationId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class NotifyReactiveEndpoint : ICommand
{
    public string CorrelationId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class BridgeToAzureCommand : ICommand
{
    public string CorrelationId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
