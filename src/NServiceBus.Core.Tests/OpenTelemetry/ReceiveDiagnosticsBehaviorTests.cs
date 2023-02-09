namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Helpers;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Features;
using Testing;
using NUnit.Framework;

[TestFixture]
class ReceiveDiagnosticsBehaviorTests
{
    [Test]
    public async Task Should_increase_total_fetched_when_processing_message()
    {
        var behavior = new ReceiveDiagnosticsBehavior("queueBaseName", "discriminator");
        var context = new TestableIncomingPhysicalMessageContext();
        context.MessageHeaders.Add(Headers.EnclosedMessageTypes, "enclosedMessageTypesString");
        context.Services.AddSingleton<ILicenseDetailsProvider>(new LicenseDetailsProvider(Guid.NewGuid().ToString(), "someCustomer"));

        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();
        await behavior.Invoke(context, _ => Task.CompletedTask);

        metricsListener.AssertMetric(Meters.TotalFetched.Name, 1);

        var fetchedTags = metricsListener.Tags[Meters.TotalFetched.Name].ToImmutableDictionary();
        Assert.AreEqual("discriminator", fetchedTags[MeterTags.EndpointDiscriminator]);
        Assert.AreEqual("queueBaseName", fetchedTags[MeterTags.QueueName]);
        Assert.AreEqual("enclosedMessageTypesString", fetchedTags[MeterTags.MessageType]);
    }

    [Test]
    public async Task Should_increase_total_successful_when_processing_message_successfully()
    {
        var behavior = new ReceiveDiagnosticsBehavior("queueBaseName", "discriminator");

        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();
        var context = new TestableIncomingPhysicalMessageContext();
        context.Services.AddSingleton<ILicenseDetailsProvider>(new LicenseDetailsProvider(Guid.NewGuid().ToString(), "someCustomer"));

        await behavior.Invoke(context, _ => Task.CompletedTask);

        metricsListener.AssertMetric(Meters.TotalFetched.Name, 1);
        metricsListener.AssertMetric(Meters.TotalProcessedSuccessfully.Name, 1);
        metricsListener.AssertMetric(Meters.TotalFailures.Name, 0);

        var processedTags = metricsListener.Tags[Meters.TotalProcessedSuccessfully.Name].ToImmutableDictionary();
        Assert.AreEqual("discriminator", processedTags[MeterTags.EndpointDiscriminator]);
        Assert.AreEqual("queueBaseName", processedTags[MeterTags.QueueName]);
        Assert.AreEqual(string.Empty, processedTags[MeterTags.MessageType], "because no message type headers is present in the message headers");
    }

    [Test]
    public void Should_increase_failures_error_when_processing_message_fails()
    {
        var behavior = new ReceiveDiagnosticsBehavior("queueBaseName", "discriminator");
        var context = new TestableIncomingPhysicalMessageContext();
        context.Services.AddSingleton<ILicenseDetailsProvider>(new LicenseDetailsProvider(Guid.NewGuid().ToString(), "someCustomer"));

        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();
        Assert.ThrowsAsync<Exception>(() => behavior.Invoke(context, _ => throw new Exception("test")));

        metricsListener.AssertMetric(Meters.TotalFetched.Name, 1);
        metricsListener.AssertMetric(Meters.TotalProcessedSuccessfully.Name, 0);
        metricsListener.AssertMetric(Meters.TotalFailures.Name, 1);

        var failureTags = metricsListener.Tags[Meters.TotalFailures.Name].ToImmutableDictionary();
        Assert.AreEqual(typeof(Exception), failureTags[MeterTags.FailureType]);
        Assert.AreEqual("discriminator", failureTags[MeterTags.EndpointDiscriminator]);
        Assert.AreEqual("queueBaseName", failureTags[MeterTags.QueueName]);
        Assert.AreEqual(string.Empty, failureTags[MeterTags.MessageType], "because no message type headers is present in the message headers");
    }

    [Test]
    public void Should_not_increase_total_failures_when_cancellation_exception()
    {
        var behavior = new ReceiveDiagnosticsBehavior("queueBaseName", "discriminator");

        using var cts = new CancellationTokenSource();
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        var context = new TestableIncomingPhysicalMessageContext { CancellationToken = cts.Token };
        context.Services.AddSingleton<ILicenseDetailsProvider>(new LicenseDetailsProvider(Guid.NewGuid().ToString(), "someCustomer"));

        cts.Cancel();
        Assert.ThrowsAsync<OperationCanceledException>(() => behavior.Invoke(context, ctx =>
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }));

        metricsListener.AssertMetric(Meters.TotalFetched.Name, 1);
        metricsListener.AssertMetric(Meters.TotalProcessedSuccessfully.Name, 0);
        metricsListener.AssertMetric(Meters.TotalFailures.Name, 0);
    }
}