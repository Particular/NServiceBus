namespace NServiceBus.Core.Tests.Pipeline;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Extensibility;
using Faults;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Pipeline;
using NUnit.Framework;
using OpenTelemetry;
using OpenTelemetry.Helpers;
using Transport;

[TestFixture]
public partial class MainPipelineExecutorTests
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

    partial class When_activity_listener_registered
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

        // In v11 the status is left unset on success: delete Should_set_ok_status_on_activity_when_pipeline_successful
        // in obsoletes-v10.cs and the attribute on this one.
        [Test]
        [OpenTelemetryV11Defaults]
        public async Task Should_leave_status_unset_on_activity_when_pipeline_successful()
        {
            var receivePipeline = new ActivityTrackingReceivePipeline();
            var serviceCollection = new ServiceCollection();
            await using var serviceProvider = serviceCollection.BuildServiceProvider();
            var executor = CreateMainPipelineExecutor(serviceProvider, receivePipeline);
            await executor.Invoke(CreateMessageContext());

            Assert.That(receivePipeline.PipelineAcitivty.Status, Is.EqualTo(ActivityStatusCode.Unset));
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

        [Test]
        public void Should_record_the_incoming_activity_as_innermost_when_pipeline_throws()
        {
            var receivePipeline = new ActivityTrackingReceivePipeline { ThrowsException = true };
            var serviceCollection = new ServiceCollection();
            using var serviceProvider = serviceCollection.BuildServiceProvider();
            var executor = CreateMainPipelineExecutor(serviceProvider, receivePipeline);
            var messageContext = CreateMessageContext();

            var exception = Assert.ThrowsAsync<Exception>(async () => await executor.Invoke(messageContext));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ExceptionTracking.For(messageContext.Extensions).TryGetInnermostActivityContext(exception, out var activityContext), Is.True);
                Assert.That(activityContext, Is.EqualTo(receivePipeline.PipelineAcitivty.Context));
            }
        }

        [Test]
        public void Should_record_only_processing_details_with_the_failed_message()
        {
            var receivePipeline = new ActivityTrackingReceivePipeline { ThrowsException = true };
            var serviceCollection = new ServiceCollection();
            using var serviceProvider = serviceCollection.BuildServiceProvider();
            var executor = CreateMainPipelineExecutor(serviceProvider, receivePipeline);
            var messageContext = CreateMessageContext();

            var exception = Assert.ThrowsAsync<Exception>(async () => await executor.Invoke(messageContext));

            var errorContext = new ErrorContext(exception, messageContext.Headers, messageContext.NativeMessageId, messageContext.Body, messageContext.TransportTransaction, 1, messageContext.ReceiveAddress, messageContext.Extensions);
            var metadata = new FaultMetadataExtractor([], _ => { }).Extract(errorContext);

            Assert.That(metadata.Keys.Where(k => k.StartsWith(FaultsHeaderKeys.ExceptionInfoDataPrefix)), Is.EquivalentTo(new[]
            {
                $"{FaultsHeaderKeys.ExceptionInfoDataPrefix}Message ID",
                $"{FaultsHeaderKeys.ExceptionInfoDataPrefix}Pipeline canceled"
            }));
        }

        [Test]
        public void Should_keep_the_innermost_activity_recorded_further_in()
        {
            using var handlerActivity = new Activity("handler");
            handlerActivity.SetIdFormat(ActivityIdFormat.W3C);
            handlerActivity.Start();
            handlerActivity.Stop();
            var exception = new Exception("handler failed");
            var messageContext = CreateMessageContext();
            ExceptionTracking.For(messageContext.Extensions).TryRecordInnermostActivity(exception, handlerActivity);

            var receivePipeline = new ActivityTrackingReceivePipeline { ExceptionToThrow = exception };
            var serviceCollection = new ServiceCollection();
            using var serviceProvider = serviceCollection.BuildServiceProvider();
            var executor = CreateMainPipelineExecutor(serviceProvider, receivePipeline);

            Assert.ThrowsAsync<Exception>(async () => await executor.Invoke(messageContext));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ExceptionTracking.For(messageContext.Extensions).TryGetInnermostActivityContext(exception, out var activityContext), Is.True);
                Assert.That(activityContext, Is.EqualTo(handlerActivity.Context));
            }
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
        var incomingPipelineMetrics = new PipelineMetrics(new TestMeterFactory(), "queue", "disc");
        var executor = new MainPipelineExecutor(
            serviceProvider,
            new PipelineCache(serviceProvider, new PipelineModifications()),
            new TestableMessageOperations(),
            new Notification<ReceivePipelineCompleted>(),
            receivePipeline,
            new ActivityFactory(new InstrumentationOptions()),
            incomingPipelineMetrics,
            new EnvelopeUnwrapper([], incomingPipelineMetrics));

        return executor;
    }

    class ActivityTrackingReceivePipeline : IPipeline<ITransportReceiveContext>
    {
        public Activity PipelineAcitivty { get; set; }

        public ITransportReceiveContext TransportReceiveContext { get; set; }

        public bool ThrowsException { get; set; }

        public Exception ExceptionToThrow { get; set; }

        public Task Invoke(ITransportReceiveContext context)
        {
            PipelineAcitivty = Activity.Current;
            TransportReceiveContext = context;

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            if (ThrowsException)
            {
                throw new Exception("Pipeline execution exception");
            }

            return Task.CompletedTask;
        }
    }
}