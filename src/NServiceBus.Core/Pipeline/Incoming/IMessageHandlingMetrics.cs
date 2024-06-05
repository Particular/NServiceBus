namespace NServiceBus;

using System;

/// <summary>
/// Registers the metrics related to a specific message handler execution.
/// </summary>
interface IMessageHandlingMetrics
{
    /// <summary>
    /// Registers the metrics related to the successful completion of the handler's execution.
    /// This method is invoked only after the handler execution completed successfully.
    /// This method will never be invoked if <c>OnFailure</c> method is invoked on the same instance.
    /// </summary>
    void OnSuccess();

    /// <summary>
    /// Registers the metrics related to the failed handler's execution.
    /// This method is invoked only after the handler execution throws an exception.
    /// This method will never be invoked if <c>OnSuccess</c> method is invoked on the same instance.
    /// </summary>
    /// <param name="error"> The exception thrown by the handler. </param>
    void OnFailure(Exception error);
}