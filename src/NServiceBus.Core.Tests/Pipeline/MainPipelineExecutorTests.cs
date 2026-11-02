namespace NServiceBus.Core.Tests.Pipeline;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Extensibility;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Pipeline;
using NUnit.Framework;
using OpenTelemetry;
using OpenTelemetry.Helpers;
using Transport;

[TestFixture]
public class MainPipelineExecutorTests
{
    [Test]
    public async Task Should_share_message_context_extension_values()
    {
        var existingValue = Guid.NewGuid();

        var receivePipeline = new TestableMessageOperations.Pipeline<ITransportReceiveContext>();
        var serviceCollection = new ServiceCollection();
        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        var executor = CreateMainPipelineExecutor(serviceProvider, receivePipeline);
        var messageContext = CreateMessageContext();
        messageContext.Extensions.Set("existing value", existingValue);
        await executor.Invoke(messageContext);

        Assert.That(receivePipeline.LastContext.Extensions.Get<Guid>("existing value"), Is.EqualTo(existingValue));
    }

    [Test]
    public async Task Should_use_message_context_extensions_as_context_root()
    {
        var newValue = Guid.NewGuid();

        var receivePipeline = new TestableMessageOperations.Pipeline<ITransportReceiveContext>
        {
            OnInvoke = ctx => ctx.Extensions.SetOnRoot("new value", newValue)
        };
        var serviceCollection = new ServiceCollection();
        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        var executor = CreateMainPipelineExecutor(serviceProvider, receivePipeline);
        var messageContext = CreateMessageContext();

        await executor.Invoke(messageContext);

        Assert.That(messageContext.Extensions.Get<Guid>("new value"), Is.EqualTo(newValue));
    }

    class When_activity_listener_registered
    {
        TestingActivityListener nsbActivityListener;

        [OneTimeSetUp]
        public void Setup()
        {
            nsbActivityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            nsbActivityListener.Dispose();
        }

        [Test]
        public async Task Should_start_Activity_when_invoking_pipeline()
        {
            var receivePipeline = new ActivityTrackingReceivePipeline();
            var serviceCollection = new ServiceCollection();
            await using var serviceProvider = serviceCollection.BuildServiceProvider();
            var executor = CreateMainPipelineExecutor(serviceProvider, receivePipeline);
            var messageContext = CreateMessageContext();

            await executor.Invoke(messageContext);

            Assert.That(receivePipeline.PipelineAcitivty, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(receivePipeline.PipelineAcitivty.OperationName, Is.EqualTo(ActivityNames.IncomingMessageActivityName));
                Assert.That(receivePipeline.PipelineAcitivty.DisplayName, Is.EqualTo("process message"));
                Assert.That(receivePipeline.TransportReceiveContext.Extensions.Get<Activity>(ActivityExtensions.IncomingActivityKey), Is.EqualTo(receivePipeline.PipelineAcitivty));
            }
        }

        [Test]
        public async Task Should_set_ok_status_on_activity_when_pipeline_successful()
        {
            var receivePipeline = new ActivityTrackingReceivePipeline();
            var serviceCollection = new ServiceCollection();
            await using var serviceProvider = serviceCollection.BuildServiceProvider();
            var executor = CreateMainPipelineExecutor(serviceProvider, receivePipeline);
            await executor.Invoke(CreateMessageContext());

            Assert.That(receivePipeline.PipelineAcitivty.Status, Is.EqualTo(ActivityStatusCode.Ok));
        }

        [Test]
        public void Should_set_error_status_on_activity_when_pipeline_throws_exception()
        {
            var receivePipeline = new ActivityTrackingReceivePipeline();
            var serviceCollection = new ServiceCollection();
            using var serviceProvider = serviceCollection.BuildServiceProvider();
            var executor = CreateMainPipelineExecutor(serviceProvider, receivePipeline);
            receivePipeline.ThrowsException = true;

            Assert.ThrowsAsync<Exception>(async () => await executor.Invoke(CreateMessageContext()));

            Assert.That(receivePipeline.PipelineAcitivty.Status, Is.EqualTo(ActivityStatusCode.Error));
        }
    }

    static MessageContext CreateMessageContext() =>
        new(
            Guid.NewGuid().ToString(),
            [],
            Array.Empty<byte>(),
            new TransportTransaction(),
            "receiver",
            new ContextBag());

    static MainPipelineExecutor CreateMainPipelineExecutor(ServiceProvider serviceProvider, IPipeline<ITransportReceiveContext> receivePipeline)
    {
        var incomingPipelineMetrics = new IncomingPipelineMetrics(new TestMeterFactory(), "queue", "disc");
        var executor = new MainPipelineExecutor(
            serviceProvider,
            new PipelineCache(serviceProvider, new PipelineModifications()),
            new TestableMessageOperations(),
            new Notification<ReceivePipelineCompleted>(),
            receivePipeline,
            new ActivityFactory(),
            incomingPipelineMetrics,
            new EnvelopeUnwrapper([], incomingPipelineMetrics));

        return executor;
    }

    class ActivityTrackingReceivePipeline : IPipeline<ITransportReceiveContext>
    {
        public Activity PipelineAcitivty { get; set; }

        public ITransportReceiveContext TransportReceiveContext { get; set; }

        public bool ThrowsException { get; set; }

        public Task Invoke(ITransportReceiveContext context)
        {
            PipelineAcitivty = Activity.Current;
            TransportReceiveContext = context;

            if (ThrowsException)
            {
                throw new Exception("Pipeline execution exception");
            }

            return Task.CompletedTask;
        }
    }
}