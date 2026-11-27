namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Collections.Generic;
using System.Linq;
using AcceptanceTests.Core.OpenTelemetry.Metrics;
using NServiceBus.Pipeline;
using NUnit.Framework;

// In v11 the execution.result tag is gone: delete this file together with the V11BehaviorSwitch block in
// obsoletes-v10.cs.
[TestFixture]
public class PipelineMetricsExecutionResultTagTests
{
    [Test]
    public void Should_emit_execution_result_tag()
    {
        var tags = RecordFailure();

        Assert.That(tags.Select(t => t.Key), Does.Contain("execution.result"));
    }

    [Test]
    [OpenTelemetryV11Defaults]
    public void Should_not_emit_execution_result_tag()
    {
        var tags = RecordFailure();

        Assert.That(tags.Select(t => t.Key), Does.Not.Contain("execution.result"));
    }

    static KeyValuePair<string, object>[] RecordFailure()
    {
        using var meterFactory = new TestMeterFactory();
        // Created before the listener subscribes, so the counters seeded with the version 11 behavior are not observed.
        var metrics = new PipelineMetrics(meterFactory, "queue", "disc");
        using var listener = TestingMetricListener.SetupNServiceBusMetricsListener();

        // As MainPipelineExecutor does: each mode creates only one of the two instruments.
        var error = new InvalidOperationException("boom");
        metrics.RecordProcessingAttempt(new PipelineMetricTags(), error);
        metrics.RecordMessageProcessingFailure(new PipelineMetricTags(), error);

        Assert.That(listener.Tags, Has.Count.EqualTo(1), "exactly one metric is expected to be recorded");
        return listener.Tags.Values.Single();
    }
}
