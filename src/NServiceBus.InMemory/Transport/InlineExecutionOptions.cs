namespace NServiceBus;

/// <summary>
/// Options for configuring inline execution behavior on the in-memory transport.
/// </summary>
public sealed class InlineExecutionOptions
{
    /// <summary>
    /// Gets or sets whether to move messages to the error queue on handler failure.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>true</c>. When set to <c>false</c>, handler exceptions will propagate to the caller
    /// instead of being handled by recoverability mechanisms.
    /// </remarks>
    public bool MoveToErrorQueueOnFailure { get; set; } = true;
}
