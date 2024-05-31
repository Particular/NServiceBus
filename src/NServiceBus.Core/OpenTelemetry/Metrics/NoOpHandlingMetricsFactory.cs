﻿namespace NServiceBus;

using System;
using Pipeline;

/// <summary>
/// Implementation of <c>IHandlingMetricsFactory</c> that does not perform anything.
/// </summary>
class NoOpHandlingMetricsFactory : IHandlingMetricsFactory
{
    /// <summary>
    /// Instantiates a new <c>IHandlingMetrics</c> that does not record any metric.
    /// </summary>
    /// <param name="context">The invocation context.</param>
    /// <returns>The instantiated <c>IHandlingMetrics</c>.</returns>
    public IHandlingMetrics StartHandling(IInvokeHandlerContext context) => new NoOpHandlingMetrics();
}

class NoOpHandlingMetrics : IHandlingMetrics
{
    public void OnSuccess()
    {
    }

    public void OnFailure(Exception error)
    {
    }
}