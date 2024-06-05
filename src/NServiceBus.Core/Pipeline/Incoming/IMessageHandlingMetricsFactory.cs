namespace NServiceBus;

using Pipeline;

/// <summary>
/// Factory for IHandlingMetrics needed to record metrics for the handler invocation.
/// </summary>
interface IMessageHandlingMetricsFactory
{
    /// <summary>
    /// Creates a new <c>IHandlingMetrics</c> instance for recording the metrics for a specific handler execution.
    /// </summary>
    /// <param name="context"> Needed to properly initialize the IHandlingMetrics instance. </param>
    /// <returns> The newly created <c>IHandlingMetrics</c> instance. </returns>
    IMessageHandlingMetrics StartHandling(IInvokeHandlerContext context);
}

