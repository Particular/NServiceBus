namespace NServiceBus.Core.Tests.Pipeline;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTests.Core.OpenTelemetry.Metrics;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public partial class MainPipelineExecutorTests
{
    const string ProcessingAttempts = "nservicebus.messaging.processing_attempts";

    [Test]
    [OpenTelemetryV11Defaults]
    public async Task Should_count_a_processing_attempt_without_error_type_when_the_pipeline_completes()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();
        await using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var executor = CreateMainPipelineExecutor(serviceProvider, new ActivityTrackingReceivePipeline());

        await executor.Invoke(CreateMessageContext());

        metricsListener.AssertMetric(ProcessingAttempts, 1);
        Assert.That(metricsListener.Tags[ProcessingAttempts].Select(t => t.Key), Does.Not.Contain("error.type"));
    }

    [Test]
    [OpenTelemetryV11Defaults]
    public async Task Should_count_a_processing_attempt_with_error_type_when_the_pipeline_fails()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();
        await using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var executor = CreateMainPipelineExecutor(serviceProvider, new ActivityTrackingReceivePipeline { ExceptionToThrow = new InvalidOperationException() });

        Assert.ThrowsAsync<InvalidOperationException>(async () => await executor.Invoke(CreateMessageContext()));

        metricsListener.AssertMetric(ProcessingAttempts, 1);
        Assert.That(metricsListener.AssertTagKeyExists(ProcessingAttempts, "error.type"), Is.EqualTo(typeof(InvalidOperationException).FullName));
    }

    [Test]
    [OpenTelemetryV11Defaults]
    public async Task Should_count_a_canceled_processing_attempt_as_operation_canceled()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();
        await using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        var executor = CreateMainPipelineExecutor(serviceProvider,
            new ActivityTrackingReceivePipeline { ExceptionToThrow = new TaskCanceledException(null, null, cancellationTokenSource.Token) });

        Assert.ThrowsAsync<TaskCanceledException>(async () => await executor.Invoke(CreateMessageContext(), cancellationTokenSource.Token));

        metricsListener.AssertMetric(ProcessingAttempts, 1);
        Assert.That(metricsListener.AssertTagKeyExists(ProcessingAttempts, "error.type"), Is.EqualTo(typeof(OperationCanceledException).FullName));
    }
}
