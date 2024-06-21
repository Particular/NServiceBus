﻿namespace NServiceBus;

using System;
using Pipeline;

/// <summary>
/// Implementation of <c>IHandlingMetricsFactory</c> that does not perform anything.
/// </summary>
class NoOpMessageHandlingMetricsFactory : IMessageHandlingMetricsFactory
{
    /// <summary>
    /// Instantiates a new <c>IHandlingMetrics</c> that does not record any metric.
    /// </summary>
    /// <param name="context">The invocation context.</param>
    /// <returns>The instantiated <c>IHandlingMetrics</c>.</returns>
    public IMessageHandlingMetrics StartHandling(IInvokeHandlerContext context) => NoOpMessageHandlingMetrics.Instance;
}

class NoOpMessageHandlingMetrics : IMessageHandlingMetrics
{
    public static IMessageHandlingMetrics Instance => new NoOpMessageHandlingMetrics();
    NoOpMessageHandlingMetrics() { }
    public void OnSuccess() { }
    public void OnFailure(Exception error) { }
}