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
        using var listener = TestingMetricListener.SetupNServiceBusMetricsListener();

        var metrics = new PipelineMetrics(meterFactory, "queue", "disc");
        metrics.RecordMessageProcessingFailure(new PipelineTelemetry(), new InvalidOperationException("boom"));

        Assert.That(listener.Tags, Has.Count.EqualTo(1), "exactly one metric is expected to be recorded");
        return listener.Tags.Values.Single();
    }
}
