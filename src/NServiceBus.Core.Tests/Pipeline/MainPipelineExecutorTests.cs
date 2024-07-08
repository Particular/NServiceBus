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
        var executor = CreateMainPipelineExecutor(receivePipeline);
        var messageContext = CreateMessageContext();
        messageContext.Extensions.Set("existing value", existingValue);
        await executor.Invoke(messageContext);

        Assert.AreEqual(existingValue, receivePipeline.LastContext.Extensions.Get<Guid>("existing value"));
    }

    [Test]
    public async Task Should_use_message_context_extensions_as_context_root()
    {
        var newValue = Guid.NewGuid();

        var receivePipeline = new TestableMessageOperations.Pipeline<ITransportReceiveContext>
        {
            OnInvoke = ctx => ctx.Extensions.SetOnRoot("new value", newValue)
        };
        var executor = CreateMainPipelineExecutor(receivePipeline);
        var messageContext = CreateMessageContext();

        await executor.Invoke(messageContext);

        Assert.AreEqual(newValue, messageContext.Extensions.Get<Guid>("new value"));
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
            var executor = CreateMainPipelineExecutor(receivePipeline);
            var messageContext = CreateMessageContext();

            await executor.Invoke(messageContext);

            Assert.NotNull(receivePipeline.PipelineAcitivty);
            Assert.AreEqual(ActivityNames.IncomingMessageActivityName, receivePipeline.PipelineAcitivty.OperationName);
            Assert.AreEqual("process message", receivePipeline.PipelineAcitivty.DisplayName);
            Assert.AreEqual(receivePipeline.PipelineAcitivty, receivePipeline.TransportReceiveContext.Extensions.Get<Activity>(ActivityExtensions.IncomingActivityKey));
        }

        [Test]
        public async Task Should_set_ok_status_on_activity_when_pipeline_successful()
        {
            var receivePipeline = new ActivityTrackingReceivePipeline();
            var executor = CreateMainPipelineExecutor(receivePipeline);

            await executor.Invoke(CreateMessageContext());

            Assert.AreEqual(ActivityStatusCode.Ok, receivePipeline.PipelineAcitivty.Status);
        }

        [Test]
        public void Should_set_error_status_on_activity_when_pipeline_throws_exception()
        {
            var receivePipeline = new ActivityTrackingReceivePipeline();
            var executor = CreateMainPipelineExecutor(receivePipeline);
            receivePipeline.ThrowsException = true;

            Assert.ThrowsAsync<Exception>(async () => await executor.Invoke(CreateMessageContext()));

            Assert.AreEqual(ActivityStatusCode.Error, receivePipeline.PipelineAcitivty.Status);
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

    static MainPipelineExecutor CreateMainPipelineExecutor(IPipeline<ITransportReceiveContext> receivePipeline)
    {
        var serviceCollection = new ServiceCollection();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var executor = new MainPipelineExecutor(
            serviceProvider,
            new PipelineCache(serviceProvider, new PipelineModifications()),
            new TestableMessageOperations(),
            new Notification<ReceivePipelineCompleted>(),
            receivePipeline,
            new ActivityFactory(),
            new IncomingPipelineMetrics(new TestMeterFactory(), "queue", "disc"));

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