namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class MainPipelineExecutorTests
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
        public async Task When_pipeline_successful()
        {
            var executor = CreateMainPipelineExecutor(out var receivePipeline);

            await executor.Invoke(CreateMessageContext());

            Assert.AreEqual(ActivityStatusCode.Ok, receivePipeline.PipelineAcitivty.Status);
        }

        [Test]
        public void When_pipeline_throws_exception()
        {
            var executor = CreateMainPipelineExecutor(out var receivePipeline);
            receivePipeline.ThrowsException = true;

            Assert.ThrowsAsync<Exception>(async () => await executor.Invoke(CreateMessageContext()));

            Assert.AreEqual(ActivityStatusCode.Error, receivePipeline.PipelineAcitivty.Status);
        }

        [Test]
        public async Task When_activity_on_context()
        {
            var executor = CreateMainPipelineExecutor(out var receivePipeline);

            using var contextActivity = CreateCompletedActivity("transport receive activity");

            var contextBag = new ContextBag();
            contextBag.Set(contextActivity);

            await executor.Invoke(CreateMessageContext(contextBag: contextBag));

            Assert.NotNull(receivePipeline.PipelineAcitivty, "should create activity for receive pipeline");
            Assert.AreEqual(contextActivity.Id, receivePipeline.PipelineAcitivty.ParentId, "should use context activity as parent");
            Assert.AreEqual(0, receivePipeline.PipelineAcitivty.Links.Count(), "should not link to logical send span");
        }

        [Test]
        public async Task When_activity_on_context_and_trace_message_header()
        {
            var executor = CreateMainPipelineExecutor(out var receivePipeline);

            using var contextActivity = CreateCompletedActivity("transport receive activity");
            using var sendActivity = CreateCompletedActivity("send activity");

            var contextBag = new ContextBag();
            contextBag.Set(contextActivity);

            var messageHeaders = new Dictionary<string, string> { { Headers.DiagnosticsTraceParent, sendActivity.Id } };

            await executor.Invoke(CreateMessageContext(messageHeaders, contextBag));

            Assert.NotNull(receivePipeline.PipelineAcitivty, "should create activity for receive pipeline");
            Assert.AreEqual(contextActivity.Id, receivePipeline.PipelineAcitivty.ParentId, "should use context activity as parent");
            Assert.AreEqual(1, receivePipeline.PipelineAcitivty.Links.Count(), "should link to logical send span");
            Assert.AreEqual(sendActivity.TraceId, receivePipeline.PipelineAcitivty.Links.Single().Context.TraceId);
            Assert.AreEqual(sendActivity.SpanId, receivePipeline.PipelineAcitivty.Links.Single().Context.SpanId);
        }

        [Test]
        public async Task When_activity_on_context_and_ambient_activity()
        {
            var executor = CreateMainPipelineExecutor(out var receivePipeline);

            using var contextActivity = CreateCompletedActivity("transport receive activity");
            var contextBag = new ContextBag();
            contextBag.Set(contextActivity);

            using var ambientActivity = ActivitySources.Main.StartActivity("ambient activity");
            Assert.AreEqual(ambientActivity, Activity.Current);

            await executor.Invoke(CreateMessageContext(contextBag: contextBag));

            Assert.NotNull(receivePipeline.PipelineAcitivty, "should create activity for receive pipeline");
            Assert.AreEqual(contextActivity.Id, receivePipeline.PipelineAcitivty.ParentId, "should use context activity as parent");
        }

        [Test]
        public async Task When_activity_on_context_uses_legacy_id_format()
        {
            var executor = CreateMainPipelineExecutor(out var receivePipeline);

            using var contextActivity = CreateCompletedActivity("transport receive activity", ActivityIdFormat.Hierarchical);
            Assert.AreEqual(ActivityIdFormat.Hierarchical, contextActivity.IdFormat);

            var contextBag = new ContextBag();
            contextBag.Set(contextActivity);

            await executor.Invoke(CreateMessageContext(contextBag: contextBag));

            Assert.NotNull(receivePipeline.PipelineAcitivty, "should create activity for receive pipeline");
            Assert.IsNull(receivePipeline.PipelineAcitivty.ParentId, "should create a new trace");
            Assert.AreEqual(ActivityIdFormat.W3C, receivePipeline.PipelineAcitivty.IdFormat);
        }

        [Test]
        public async Task When_no_activity_on_context_and_trace_message_header()
        {
            var executor = CreateMainPipelineExecutor(out var receivePipeline);

            using var sendActivity = CreateCompletedActivity("send activity");

            var messageHeaders = new Dictionary<string, string> { { Headers.DiagnosticsTraceParent, sendActivity.Id } };

            await executor.Invoke(CreateMessageContext(messageHeaders));

            Assert.NotNull(receivePipeline.PipelineAcitivty, "should create activity for receive pipeline");
            Assert.AreEqual(sendActivity.Id, receivePipeline.PipelineAcitivty.ParentId);
            Assert.AreEqual(0, receivePipeline.PipelineAcitivty.Links.Count(), "should not link to logical send span");
        }

        [TestCase(ActivityIdFormat.W3C)]
        [TestCase(ActivityIdFormat.Hierarchical)]
        public async Task When_no_activity_on_context_and_no_trace_message_header_and_ambient_activity(ActivityIdFormat ambientActivityIdFormat)
        {
            var executor = CreateMainPipelineExecutor(out var receivePipeline);

            using var activity = new Activity("ambient activity");
            activity.SetIdFormat(ambientActivityIdFormat);
            activity.Start();

            await executor.Invoke(CreateMessageContext());

            Assert.NotNull(receivePipeline.PipelineAcitivty, "should create activity for receive pipeline");
            Assert.AreEqual(activity.Id, receivePipeline.PipelineAcitivty.ParentId, "should attach to ambient activity");
            Assert.AreEqual(ActivityIdFormat.W3C, receivePipeline.PipelineAcitivty.IdFormat);
        }

        [Test]
        public async Task When_no_activity_on_context_and_no_trace_message_header_and_no_ambient_activity()
        {
            var executor = CreateMainPipelineExecutor(out var receivePipeline);

            await executor.Invoke(CreateMessageContext());

            Assert.NotNull(receivePipeline.PipelineAcitivty, "should create activity for receive pipeline");
            Assert.IsNull(receivePipeline.PipelineAcitivty.ParentId, "should start a new trace");
            Assert.AreEqual(ActivityIdFormat.W3C, receivePipeline.PipelineAcitivty.IdFormat);
        }

        [Test]
        public async Task When_trace_header_contains_invalid_data()
        {
            var executor = CreateMainPipelineExecutor(out var receivePipeline);

            var messageHeaders = new Dictionary<string, string> { { Headers.DiagnosticsTraceParent, "Some invalid traceparent format" } };

            await executor.Invoke(CreateMessageContext(messageHeaders));

            Assert.NotNull(receivePipeline.PipelineAcitivty, "should create activity for receive pipeline");
            Assert.IsNull(receivePipeline.PipelineAcitivty.ParentId, "should start new trace");
            Assert.AreEqual(0, receivePipeline.PipelineAcitivty.Links.Count(), "should not link to logical send span");
        }

        Activity CreateCompletedActivity(string activityName, ActivityIdFormat idFormat = ActivityIdFormat.W3C)
        {
            var activity = new Activity(activityName);
            activity.SetIdFormat(idFormat);
            activity.Start();
            activity.Stop();
            return activity;
        }

        static MessageContext CreateMessageContext(Dictionary<string, string> messageHeaders = null, ContextBag contextBag = null)
        {
            return new MessageContext(
                Guid.NewGuid().ToString(),
                messageHeaders ?? new Dictionary<string, string>(),
                Array.Empty<byte>(),
                new TransportTransaction(),
                "receiver",
                contextBag ?? new ContextBag());
        }

        static MainPipelineExecutor CreateMainPipelineExecutor(out ReceivePipeline receivePipeline)
        {
            var serviceCollection = new ServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            receivePipeline = new ReceivePipeline();
            var executor = new MainPipelineExecutor(
                serviceProvider,
                new PipelineCache(serviceProvider, new PipelineModifications()),
                new MessageOperations(null, null, null, null, null, null),
                new Notification<ReceivePipelineCompleted>(),
                receivePipeline);

            return executor;
        }

        class ReceivePipeline : IPipeline<ITransportReceiveContext>
        {
            public Activity PipelineAcitivty { get; set; }

            public bool ThrowsException { get; set; }

            public Task Invoke(ITransportReceiveContext context)
            {
                PipelineAcitivty = Activity.Current;

                if (ThrowsException)
                {
                    throw new Exception("Pipeline execution exception");
                }

                return Task.CompletedTask;
            }
        }

        class TestingActivityListener : IDisposable
        {
            readonly ActivityListener activityListener;

            public static TestingActivityListener SetupNServiceBusDiagnosticListener() => SetupDiagnosticListener("NServiceBus.Diagnostics");

            public static TestingActivityListener SetupDiagnosticListener(string sourceName)
            {
                var testingListener = new TestingActivityListener(sourceName);

                ActivitySource.AddActivityListener(testingListener.activityListener);
                return testingListener;
            }

            TestingActivityListener(string sourceName = null)
            {
                // do not rely on activities from the notifications as tests are run in parallel
                activityListener = new ActivityListener
                {
                    ShouldListenTo = source => string.IsNullOrEmpty(sourceName) || source.Name == sourceName,
                    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                    SampleUsingParentId = (ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllData
                };
            }
            public void Dispose() => activityListener?.Dispose();
        }
    }
}