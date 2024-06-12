#nullable enable
namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

/// <summary>
/// Creates the Metrics' bag for a specific pipeline execution.
/// </summary>
interface IMetricsFactory
{
    /// <summary>
    /// Create a metrics' bag for a specific pipeline execution.
    /// </summary>
    /// <returns>The newly created metrics bag</returns>
    IMetricsBag? MetricsBag() => null;
}

/// <summary>
/// Collect the metrics related to a specific pipeline execution.
/// </summary>
interface IMetricsBag
{
    /// <summary>
    /// Starts the recording the duration of a specific step execution.
    /// </summary>
    /// <param name="meter">The meter used for the specific step.</param>
    /// <param name="tags">The additional tags to be added to all the metrics in this bag.</param>
    /// <returns></returns>
    IExecutionMetric StartExecution(Histogram<double> meter, params KeyValuePair<string, object>[] tags);

    /// <summary>
    /// Record all the metrics in this bag.
    /// </summary>
    /// <param name="tags">The additional tags to be added to all the metrics in this bag.</param>
    void Record(params KeyValuePair<string, object>[] tags);
}

/// <summary>
/// The metric of the execution of specific pipeline step/operation.
/// </summary>
interface IExecutionMetric
{
    /// <summary>
    /// Signals the completion of the execution related to this metric.
    /// This method is invoked only after the execution completed successfully.
    /// This method will never be invoked if <c>OnFailure</c> method is invoked on the same instance.
    /// <param name="tags">The tags to add the specific metric only.</param>
    /// </summary>
    void OnSuccess(params KeyValuePair<string, object>[] tags);

    /// <summary>
    /// Signals the failure of the execution related to this metric.
    /// This method is invoked only after the execution throws an exception.
    /// This method will never be invoked if <c>OnSuccess</c> method is invoked on the same instance.
    /// </summary>
    /// <param name="error"> The exception thrown during the execution. </param>
    /// <param name="tags"> The tags to add the specific metric only. </param>
    void OnFailure(Exception error, params KeyValuePair<string, object>[] tags);
}