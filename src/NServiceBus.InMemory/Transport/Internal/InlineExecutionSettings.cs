namespace NServiceBus;

sealed class InlineExecutionSettings
{
    public static readonly InlineExecutionSettings Disabled = new();
    public bool IsEnabled { get; }
    public bool MoveToErrorQueueOnFailure { get; }

    public InlineExecutionSettings(InlineExecutionOptions options)
    {
        IsEnabled = true;
        MoveToErrorQueueOnFailure = options.MoveToErrorQueueOnFailure;
    }

    InlineExecutionSettings()
    {
        IsEnabled = false;
        MoveToErrorQueueOnFailure = true;
    }
}