namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

class MetricsFactory(string endpoint, string discriminator) : IMetricsFactory
{
    public IMetricsBag MetricsBag() => new MetricBag(endpoint, discriminator);
}

class MetricBag(string endpoint, string discriminator) : IMetricsBag
{
    public IExecutionMetric StartExecution(Histogram<double> meter, params KeyValuePair<string, object>[] tags)
    {
        foreach (KeyValuePair<string, object> keyValuePair in tags)
        {
            tagList.Add(keyValuePair);
        }

        var executionMetric = new ExecutionMetric(meter);
        metrics.Add(executionMetric);
        return executionMetric;
    }

    public void Record(params KeyValuePair<string, object>[] tags)
    {
        foreach (KeyValuePair<string, object> keyValuePair in tags)
        {
            tagList.Add(keyValuePair);
        }
        metrics.ForEach(metric => metric.Record(tagList));
    }

    readonly List<ExecutionMetric> metrics = [];
    TagList tagList = MeterTags.CommonMessagingMetricTags(endpoint, discriminator);
}

class ExecutionMetric : IExecutionMetric
{
    public ExecutionMetric(Histogram<double> meter)
    {
        this.meter = meter;
        stopWatch.Start();
    }

    public void OnSuccess(params KeyValuePair<string, object>[] tags)
    {
        stopWatch.Stop();
        foreach (KeyValuePair<string, object> keyValuePair in tags)
        {
            tagList.Add(keyValuePair);
        }
        tagList.Add(new KeyValuePair<string, object>(MeterTags.ExecutionResult, "success"));
    }

    public void OnFailure(Exception error, params KeyValuePair<string, object>[] tags)
    {
        stopWatch.Stop();
        foreach (KeyValuePair<string, object> keyValuePair in tags)
        {
            tagList.Add(keyValuePair);
        }
        tagList.Add(new KeyValuePair<string, object>(MeterTags.ExecutionResult, "failure"));
        tagList.Add(new KeyValuePair<string, object>(MeterTags.ErrorType, error.GetType().FullName));
    }

    internal void Record(TagList tags)
    {
        foreach (KeyValuePair<string, object> keyValuePair in tags)
        {
            tagList.Add(keyValuePair);
        }
        meter.Record(stopWatch.Elapsed.TotalSeconds, tagList);
    }

    readonly Stopwatch stopWatch = new();
    readonly Histogram<double> meter;
    TagList tagList;
}