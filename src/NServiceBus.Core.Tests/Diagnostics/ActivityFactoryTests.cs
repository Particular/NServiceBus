namespace NServiceBus.Core.Tests.Diagnostics;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Extensibility;
using Helpers;
using NServiceBus.Pipeline;
using NServiceBus.Sagas;
using NUnit.Framework;
using Transport;

[TestFixture]
public class ActivityFactoryTests
{
    ActivityFactory activityFactory = new();

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

    class StartIncomingActivity : ActivityFactoryTests
    {
        [Test]
        public void Should_attach_to_context_activity_when_activity_on_context()
        {
            using var contextActivity = CreateCompletedActivity("transport receive activity");

            var contextBag = new ContextBag();
            contextBag.Set(contextActivity);

            var activity = activityFactory.StartIncomingActivity(CreateMessageContext(contextBag: contextBag));

            Assert.NotNull(activity, "should create activity for receive pipeline");
            Assert.AreEqual(contextActivity.Id, activity.ParentId, "should use context activity as parent");
            Assert.AreEqual(0, activity.Links.Count(), "should not link to logical send span");
        }

        [Test]
        public void Should_attach_to_context_activity_when_activity_on_context_and_trace_message_header()
        {
            using var contextActivity = CreateCompletedActivity("transport receive activity");
            using var sendActivity = CreateCompletedActivity("send activity");

            var contextBag = new ContextBag();
            contextBag.Set(contextActivity);

            var messageHeaders = new Dictionary<string, string> { { Headers.DiagnosticsTraceParent, sendActivity.Id } };

            var activity = activityFactory.StartIncomingActivity(CreateMessageContext(messageHeaders, contextBag));

            Assert.NotNull(activity, "should create activity for receive pipeline");
            Assert.AreEqual(contextActivity.Id, activity.ParentId, "should use context activity as parent");
            Assert.AreEqual(1, activity.Links.Count(), "should link to logical send span");
            Assert.AreEqual(sendActivity.TraceId, activity.Links.Single().Context.TraceId);
            Assert.AreEqual(sendActivity.SpanId, activity.Links.Single().Context.SpanId);
        }

        [Test]
        public void Should_attach_to_context_activity_when_activity_on_context_and_ambient_activity()
        {
            using var contextActivity = CreateCompletedActivity("transport receive activity");
            var contextBag = new ContextBag();
            contextBag.Set(contextActivity);

            using var ambientActivity = ActivitySources.Main.StartActivity("ambient activity");
            Assert.AreEqual(ambientActivity, Activity.Current);

            var activity = activityFactory.StartIncomingActivity(CreateMessageContext(contextBag: contextBag));

            Assert.NotNull(activity, "should create activity for receive pipeline");
            Assert.AreEqual(contextActivity.Id, activity.ParentId, "should use context activity as parent");
        }

        [Test]
        public void Should_start_new_trace_when_activity_on_context_uses_legacy_id_format()
        {
            using var contextActivity = CreateCompletedActivity("transport receive activity", ActivityIdFormat.Hierarchical);
            Assert.AreEqual(ActivityIdFormat.Hierarchical, contextActivity.IdFormat);

            var contextBag = new ContextBag();
            contextBag.Set(contextActivity);

            var activity = activityFactory.StartIncomingActivity(CreateMessageContext(contextBag: contextBag));

            Assert.NotNull(activity, "should create activity for receive pipeline");
            Assert.IsNull(activity.ParentId, "should create a new trace");
            Assert.AreEqual(ActivityIdFormat.W3C, activity.IdFormat);
        }

        [Test]
        public void Should_attach_to_header_trace_when_no_activity_on_context_and_trace_header()
        {
            using var sendActivity = CreateCompletedActivity("send activity");

            var messageHeaders = new Dictionary<string, string> { { Headers.DiagnosticsTraceParent, sendActivity.Id } };

            var activity = activityFactory.StartIncomingActivity(CreateMessageContext(messageHeaders));

            Assert.NotNull(activity, "should create activity for receive pipeline");
            Assert.AreEqual(sendActivity.Id, activity.ParentId);
            Assert.AreEqual(0, activity.Links.Count(), "should not link to logical send span");
        }

        [TestCase(ActivityIdFormat.W3C)]
        [TestCase(ActivityIdFormat.Hierarchical)]
        public void Should_attach_to_ambient_trace_when_no_activity_on_context_and_no_trace_header_and_ambient_activity(ActivityIdFormat ambientActivityIdFormat)
        {
            using var ambientActivity = new Activity("ambient activity");
            ambientActivity.SetIdFormat(ambientActivityIdFormat);
            ambientActivity.Start();

            var activity = activityFactory.StartIncomingActivity(CreateMessageContext());

            Assert.NotNull(activity, "should create activity for receive pipeline");
            Assert.AreEqual(ambientActivity.Id, activity.ParentId, "should attach to ambient activity");
            Assert.AreEqual(ActivityIdFormat.W3C, activity.IdFormat);
        }

        [Test]
        public void Should_start_new_trace_when_no_activity_on_context_and_no_trace_message_header_and_no_ambient_activity()
        {
            var activity = activityFactory.StartIncomingActivity(CreateMessageContext());

            Assert.NotNull(activity, "should create activity for receive pipeline");
            Assert.IsNull(activity.ParentId, "should start a new trace");
            Assert.AreEqual(ActivityIdFormat.W3C, activity.IdFormat);
        }

        [Test]
        public void Should_start_new_trace_when_trace_header_contains_invalid_data()
        {
            var messageHeaders = new Dictionary<string, string> { { Headers.DiagnosticsTraceParent, "Some invalid traceparent format" } };

            var activity = activityFactory.StartIncomingActivity(CreateMessageContext(messageHeaders));

            Assert.NotNull(activity, "should create activity for receive pipeline");
            Assert.IsNull(activity.ParentId, "should start new trace");
            Assert.AreEqual(0, activity.Links.Count(), "should not link to logical send span");
        }

        [Test]
        public void Should_add_native_message_id_tag()
        {
            MessageContext messageContext = CreateMessageContext();

            var activity = activityFactory.StartIncomingActivity(messageContext);

            Assert.AreEqual(messageContext.NativeMessageId, activity.Tags.ToImmutableDictionary()["nservicebus.native_message_id"]);
        }

        static Activity CreateCompletedActivity(string activityName, ActivityIdFormat idFormat = ActivityIdFormat.W3C)
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
    }

    class StartOutgoingPipelineActivity : ActivityFactoryTests
    {
        [Test]
        public void Should_attach_ambient_activity()
        {
            using var ambientActivity = new Activity("ambient activity");
            ambientActivity.Start();

            var activity = activityFactory.StartOutgoingPipelineActivity("activityName", "activityDisplayName", new FakeRootContext());

            Assert.AreEqual(ambientActivity.Id, activity.ParentId);
        }

        [Test]
        public void Should_always_create_a_w3c_id()
        {
            using var ambientActivity = new Activity("ambient activity");
            ambientActivity.SetIdFormat(ActivityIdFormat.Hierarchical); // when caller is running on .NET Framework without ambient W3C id format activity
            ambientActivity.Start();

            var activity = activityFactory.StartOutgoingPipelineActivity("activityName", "activityDisplayName", new FakeRootContext());

            Assert.AreEqual(ambientActivity.Id, activity.ParentId);
            Assert.AreEqual(ActivityIdFormat.W3C, activity.IdFormat);
        }

        [Test]
        public void Should_set_activity_in_context()
        {
            var context = new FakeRootContext();
            activityFactory.StartOutgoingPipelineActivity("activityName", "activityDisplayName", context);

            Assert.IsNotNull(context.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey));
        }
    }

    class StartHandlerActivity : ActivityFactoryTests
    {
        [Test]
        public void Should_set_handler_type_as_tag()
        {
            Type handlerType = typeof(StartHandlerActivity);
            var activity = activityFactory.StartHandlerActivity(new MessageHandler((_, _, _) => Task.CompletedTask, handlerType), null);

            Assert.IsNotNull(activity);
            var tags = activity.Tags.ToImmutableDictionary();
            Assert.AreEqual(handlerType.FullName, tags[ActivityTags.HandlerType]);
        }

        [Test]
        public void Should_set_saga_id_when_saga()
        {
            var sagaInstance = new ActiveSagaInstance(null, null, () => DateTimeOffset.UtcNow) { SagaId = Guid.NewGuid().ToString() };

            var activity = activityFactory.StartHandlerActivity(new MessageHandler((_, _, _) => Task.CompletedTask, typeof(StartHandlerActivity)), sagaInstance);

            Assert.IsNotNull(activity);
            var tags = activity.Tags.ToImmutableDictionary();

            Assert.AreEqual(sagaInstance.SagaId, tags[ActivityTags.HandlerSagaId]);
        }
    }
}