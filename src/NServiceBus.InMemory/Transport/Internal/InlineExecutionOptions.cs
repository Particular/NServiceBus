namespace NServiceBus;

public sealed class InlineExecutionOptions
{
    internal static readonly InlineExecutionOptions Disabled = new();

    public bool MoveToErrorQueueOnFailure { get; set; } = true;
}