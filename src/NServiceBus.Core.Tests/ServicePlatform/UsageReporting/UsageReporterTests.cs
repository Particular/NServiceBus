namespace NServiceBus.Core.Tests.ServicePlatform.UsageReporting;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NServiceBus.ServicePlatform;
using NServiceBus.Testing;
using NUnit.Framework;

[TestFixture]
public class UsageReporterTests
{
    FakeTimeProvider fakeTimeProvider;
    UsageReporterSettings settings;
    FakeServicePlatformChannel fakeChannel;
    FakeServicePlatformSender<EndpointUsageReport> fakeUsageReportSender;
    FakePipeline fakePipeline;
    UsageReporter usageReporter;

    List<EndpointUsageReport> UsageReports => fakeUsageReportSender.SentMessages;
    long TotalReportedUsage => UsageReports.Sum(x => x.MessagesSuccessfullyProcessed);

    [SetUp]
    public async Task Setup()
    {
        fakeTimeProvider = new();
        settings = new()
        {
            EndpointName = "TestEndpoint",
            BaseQueueAddress = "TestQueue",
            ReportingInterval = TimeSpan.FromMinutes(10)
        };

        fakeUsageReportSender = new();
        fakeChannel = new(fakeUsageReportSender);

        fakePipeline = FakePipeline.Build(settings.BaseQueueAddress);
        usageReporter = new(fakeChannel, settings, fakeTimeProvider, NullLogger<UsageReporter>.Instance);

        await usageReporter.PerformStartup(null);
    }

    [TearDown]
    public async Task TearDown()
    {
        await usageReporter.PerformStop(null);
        usageReporter.Dispose();
    }

    [Test]
    public void Should_send_usage_report_periodically()
    {
        SimulateSuccessfulMessages(3);
        AdvanceToNextReportingInterval();

        SimulateSuccessfulMessages(2);
        AdvanceToNextReportingInterval();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(UsageReports, Has.Count.EqualTo(2));
            Assert.That(TotalReportedUsage, Is.EqualTo(5));
        }
    }

    [Test]
    public void Should_ignore_throughput_from_other_endpoints()
    {
        var otherEndpoint = FakePipeline.Build("OtherEndpointQueue");

        SimulateSuccessfulMessages(2);
        SimulateSuccessfulMessages(3, otherEndpoint);
        AdvanceToNextReportingInterval();

        SimulateSuccessfulMessages(4);
        SimulateSuccessfulMessages(5, otherEndpoint);
        AdvanceToNextReportingInterval();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(UsageReports, Has.Count.EqualTo(2));
            Assert.That(TotalReportedUsage, Is.EqualTo(6));
        }
    }

    [Test]
    public async Task Should_send_usage_report_on_graceful_shutdown()
    {
        SimulateSuccessfulMessages(2);

        await GracefulShutdown();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(UsageReports, Has.Count.EqualTo(1));
            Assert.That(TotalReportedUsage, Is.EqualTo(2));
        }
    }

    [Test]
    public void Should_send_zero_even_if_no_messages_processed()
    {
        AdvanceToNextReportingInterval();
        AdvanceToNextReportingInterval();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(UsageReports, Has.Count.EqualTo(2));
            Assert.That(TotalReportedUsage, Is.Zero);
        }
    }

    void AdvanceToNextReportingInterval() => fakeTimeProvider.Advance(settings.ReportingInterval);
    void SimulateSuccessfulMessages(int n = 1, FakePipeline pipeline = null) => (pipeline ?? fakePipeline).SimulateSuccessfullyProcessedMessages(n);
    Task GracefulShutdown(CancellationToken cancellationToken = default) => usageReporter.PerformStop(null, cancellationToken);

    class FakePipeline(IncomingPipelineMetrics metrics)
    {
        public static FakePipeline Build(string queueName = "QueueName", string discriminator = "Discriminator")
        {
            var serviceProvider = new ServiceCollection()
                .AddMetrics()
                .BuildServiceProvider();
            var meterFactory = serviceProvider.GetRequiredService<IMeterFactory>();

            var incomingPipelineMetrics = new IncomingPipelineMetrics(meterFactory, queueName, discriminator);

            return new(incomingPipelineMetrics);
        }

        public void SimulateSuccessfullyProcessedMessages(int n)
        {
            for (var i = 0; i < n; i++)
            {
                var context = new TestableTransportReceiveContext();
                var metricsTags = context.Extensions.GetOrCreate<IncomingPipelineMetricTags>();
                metrics.AddDefaultIncomingPipelineMetricTags(metricsTags);
                metrics.RecordCriticalTimeAndTotalProcessed(context);
            }
        }
    }

    class FakeServicePlatformChannel(params object[] senders) : ServicePlatformChannel
    {
        public override ServicePlatformSender<TMessage> CreateSender<TMessage>(JsonTypeInfo<TMessage> jsonTypeInfo)
            => senders.OfType<ServicePlatformSender<TMessage>>().Single();
    }

    class FakeServicePlatformSender<TMessage> : ServicePlatformSender<TMessage>
    {
        public List<TMessage> SentMessages { get; } = [];

        public override Task Send(TMessage message, CancellationToken cancellationToken = default)
        {
            SentMessages.Add(message);
            return Task.CompletedTask;
        }
    }
}
