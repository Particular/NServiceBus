namespace NServiceBus;

sealed class InlineExecutionSettings
{
    public static readonly InlineExecutionSettings Disabled = new();
    public bool IsEnabled { get; }
    public bool MoveToErrorQueueOnFailure { get; }

    public InlineExecutionSettings(InMemoryTransportOptions options)
    {
        IsEnabled = options.InlineExecution is not null;
        MoveToErrorQueueOnFailure = options.InlineExecution?.MoveToErrorQueueOnFailure ?? true;
    }

    InlineExecutionSettings()
    {
        IsEnabled = false;
        MoveToErrorQueueOnFailure = true;
    }
}