#nullable enable

namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Helpers;
using NServiceBus.Extensibility;
using NServiceBus.Pipeline;
using NServiceBus.Sagas;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class ActivityFactoryTests
{
    readonly ActivityFactory activityFactory = new();

    TestingActivityListener nsbActivityListener;

    [OneTimeSetUp]
    public void Setup() => nsbActivityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();

    [OneTimeTearDown]
    public void TearDown() => nsbActivityListener.Dispose();

    class NoDiagnosticListeners
    {
        readonly ActivityFactory activityFactory = new();

        [Test]
        public void Should_return_null_incoming_activity_when_no_listeners()
        {
            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext());
            Assert.That(activity, Is.Null, "should return null when no listeners");
        }

        [Test]
        public void Should_return_null_outgoing_activity_when_no_listeners()
        {
            var activity = activityFactory.StartOutgoingPipelineActivity("activityName", "activityDisplayName", new FakeRootContext());
            Assert.That(activity, Is.Null, "should return null when no listeners");
        }

        static MessageContext CreateMessageContext() =>
            new(Guid.NewGuid().ToString(), [], Array.Empty<byte>(), new TransportTransaction(), "receiver", new ContextBag());
    }

    class StartIncomingActivity : ActivityFactoryTests
    {
        [Test]
        public void Should_attach_to_context_activity_when_activity_on_context()
        {
            using var contextActivity = CreateCompletedActivity("transport receive activity");

            var contextBag = new ContextBag();
            contextBag.Set(contextActivity);

            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext(contextBag: contextBag));

            Assert.That(activity, Is.Not.Null, "should create activity for receive pipeline");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(activity.ParentId, Is.EqualTo(contextActivity.Id), "should use context activity as parent");
                Assert.That(activity.Links.Count(), Is.EqualTo(0), "should not link to logical send span");
            }
        }

        [Test]
        public void Should_attach_to_context_activity_when_activity_on_context_and_trace_message_header()
        {
            using var contextActivity = CreateCompletedActivity("transport receive activity");
            using var sendActivity = CreateCompletedActivity("send activity");

            var contextBag = new ContextBag();
            contextBag.Set(contextActivity);

            var messageHeaders = new Dictionary<string, string> { { Headers.DiagnosticsTraceParent, sendActivity.Id! } };

            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext(messageHeaders, contextBag));

            Assert.That(activity, Is.Not.Null, "should create activity for receive pipeline");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(activity.ParentId, Is.EqualTo(contextActivity.Id), "should use context activity as parent");
                Assert.That(activity.Links.Count(), Is.EqualTo(1), "should link to logical send span");
                Assert.That(activity.Links.Single().Context.TraceId, Is.EqualTo(sendActivity.TraceId));
                Assert.That(activity.Links.Single().Context.SpanId, Is.EqualTo(sendActivity.SpanId));
            }
        }

        [Test]
        public void Should_attach_to_context_activity_when_activity_on_context_and_ambient_activity()
        {
            using var contextActivity = CreateCompletedActivity("transport receive activity");
            var contextBag = new ContextBag();
            contextBag.Set(contextActivity);

            using var ambientActivity = ActivitySources.Main.StartActivity("ambient activity");
            Assert.That(Activity.Current, Is.EqualTo(ambientActivity));

            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext(contextBag: contextBag));

            Assert.That(activity, Is.Not.Null, "should create activity for receive pipeline");
            Assert.That(activity.ParentId, Is.EqualTo(contextActivity.Id), "should use context activity as parent");
        }

        [Test]
        public void Should_start_new_trace_when_activity_on_context_uses_legacy_id_format()
        {
            using var contextActivity = CreateCompletedActivity("transport receive activity", ActivityIdFormat.Hierarchical);
            Assert.That(contextActivity.IdFormat, Is.EqualTo(ActivityIdFormat.Hierarchical));

            var contextBag = new ContextBag();
            contextBag.Set(contextActivity);

            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext(contextBag: contextBag));

            Assert.That(activity, Is.Not.Null, "should create activity for receive pipeline");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(activity.ParentId, Is.Null, "should create a new trace");
                Assert.That(activity.IdFormat, Is.EqualTo(ActivityIdFormat.W3C));
            }
        }

        [Test]
        public void Should_attach_to_header_trace_when_no_activity_on_context_and_trace_header()
        {
            using var sendActivity = CreateCompletedActivity("send activity");

            var messageHeaders = new Dictionary<string, string> { { Headers.DiagnosticsTraceParent, sendActivity.Id! } };

            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext(messageHeaders));

            Assert.That(activity, Is.Not.Null, "should create activity for receive pipeline");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(activity.ParentId, Is.EqualTo(sendActivity.Id));
                Assert.That(activity.Links.Count(), Is.EqualTo(0), "should not link to logical send span");
            }
        }

        [TestCase(ActivityIdFormat.W3C)]
        [TestCase(ActivityIdFormat.Hierarchical)]
        public void Should_attach_to_ambient_trace_when_no_activity_on_context_and_no_trace_header_and_ambient_activity(ActivityIdFormat ambientActivityIdFormat)
        {
            using var ambientActivity = new Activity("ambient activity");
            ambientActivity.SetIdFormat(ambientActivityIdFormat);
            ambientActivity.Start();

            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext());

            Assert.That(activity, Is.Not.Null, "should create activity for receive pipeline");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(activity.ParentId, Is.EqualTo(ambientActivity.Id), "should attach to ambient activity");
                Assert.That(activity.IdFormat, Is.EqualTo(ActivityIdFormat.W3C));
            }
        }

        [Test]
        public void Should_start_new_trace_when_no_activity_on_context_and_no_trace_message_header_and_no_ambient_activity()
        {
            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext());

            Assert.That(activity, Is.Not.Null, "should create activity for receive pipeline");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(activity.ParentId, Is.Null, "should start a new trace");
                Assert.That(activity.IdFormat, Is.EqualTo(ActivityIdFormat.W3C));
            }
        }

        [Test]
        public void Should_start_new_trace_when_trace_header_contains_invalid_data()
        {
            var messageHeaders = new Dictionary<string, string> { { Headers.DiagnosticsTraceParent, "Some invalid traceparent format" } };

            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext(messageHeaders));

            Assert.That(activity, Is.Not.Null, "should create activity for receive pipeline");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(activity.ParentId, Is.Null, "should start new trace");
                Assert.That(activity.Links.Count(), Is.EqualTo(0), "should not link to logical send span");
            }
        }

        [Test]
        public void Should_add_native_message_id_tag()
        {
            MessageContext messageContext = CreateMessageContext();

            var activity = activityFactory.StartIncomingPipelineActivity(messageContext);

            Assert.That(activity!.Tags.ToImmutableDictionary()["nservicebus.native_message_id"], Is.EqualTo(messageContext.NativeMessageId));
        }

        static Activity CreateCompletedActivity(string activityName, ActivityIdFormat idFormat = ActivityIdFormat.W3C)
        {
            var activity = new Activity(activityName);
            activity.SetIdFormat(idFormat);
            activity.Start();
            activity.Stop();
            return activity;
        }

        static MessageContext CreateMessageContext(Dictionary<string, string>? messageHeaders = null, ContextBag? contextBag = null) =>
            new(
                Guid.NewGuid().ToString(),
                messageHeaders ?? [],
                Array.Empty<byte>(),
                new TransportTransaction(),
                "receiver",
                contextBag ?? new ContextBag());
    }

    class StartOutgoingPipelineActivity : ActivityFactoryTests
    {
        [Test]
        public void Should_attach_ambient_activity()
        {
            using var ambientActivity = new Activity("ambient activity");
            ambientActivity.Start();

            var activity = activityFactory.StartOutgoingPipelineActivity("activityName", "activityDisplayName", new FakeRootContext());

            Assert.That(activity?.ParentId, Is.EqualTo(ambientActivity.Id));
        }

        [Test]
        public void Should_always_create_a_w3c_id()
        {
            using var ambientActivity = new Activity("ambient activity");
            ambientActivity.SetIdFormat(ActivityIdFormat.Hierarchical); // when caller is running on .NET Framework without ambient W3C id format activity
            ambientActivity.Start();

            var activity = activityFactory.StartOutgoingPipelineActivity("activityName", "activityDisplayName", new FakeRootContext());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(activity?.ParentId, Is.EqualTo(ambientActivity.Id));
                Assert.That(activity?.IdFormat, Is.EqualTo(ActivityIdFormat.W3C));
            }
        }

        [Test]
        public void Should_set_activity_in_context()
        {
            var context = new FakeRootContext();
            _ = activityFactory.StartOutgoingPipelineActivity("activityName", "activityDisplayName", context);

            Assert.That(context.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey), Is.Not.Null);
        }
    }

    class StartHandlerActivity : ActivityFactoryTests
    {
        [Test]
        public void Should_not_start_activity_when_no_parent_activity_exists()
        {
            Type handlerType = typeof(StartHandlerActivity);
            var activity = activityFactory.StartHandlerActivity(new MessageHandler { HandlerType = handlerType });

            Assert.That(activity, Is.Null, "should not start handler activity when no parent activity exists");
        }

        [Test]
        public void Should_set_handler_type_as_tag()
        {
            Type handlerType = typeof(StartHandlerActivity);

            using var ambientActivity = new Activity("ambient activity");
            ambientActivity.Start();

            var activity = activityFactory.StartHandlerActivity(new MessageHandler { HandlerType = handlerType });

            Assert.That(activity, Is.Not.Null);
            var tags = activity.Tags.ToImmutableDictionary();
            Assert.That(tags[ActivityTags.HandlerType], Is.EqualTo(handlerType.FullName));
        }

        [Test]
        public void Should_set_saga_id_when_saga()
        {
            var sagaInstance = new ActiveSagaInstance(null, null, () => DateTimeOffset.UtcNow) { SagaId = Guid.NewGuid().ToString() };

            using var ambientActivity = new Activity("ambient activity");
            ambientActivity.Start();

            var activity = activityFactory.StartHandlerActivity(new MessageHandler { HandlerType = typeof(StartHandlerActivity) });

            Assert.That(activity, Is.Not.Null);
        }
    }
}