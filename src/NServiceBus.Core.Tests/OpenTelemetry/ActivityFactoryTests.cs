#nullable enable

namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Helpers;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Extensibility;
using NServiceBus.Pipeline;
using NServiceBus.Sagas;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class ActivityFactoryTests
{
    readonly ActivityFactory activityFactory = new(new InstrumentationOptions());

    TestingActivityListener nsbActivityListener;

    [OneTimeSetUp]
    public void Setup() => nsbActivityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();

    [OneTimeTearDown]
    public void TearDown() => nsbActivityListener.Dispose();

    class NoDiagnosticListeners
    {
        readonly ActivityFactory activityFactory = new(new InstrumentationOptions());

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

    // Until v11 the "transport span as parent" behavior is opt-in. This fixture runs with the v11
    // defaults because that is what the tests below describe; the pre-v11 default is covered by
    // TransportParentSpanDefaultBehaviorTests. In v11, remove the attribute together with the
    // V11BehaviorSwitch block in obsoletes-v10.cs.
    [OpenTelemetryV11Defaults]
    class StartIncomingActivity : ActivityFactoryTests
    {
        [TestCase(Headers.NServiceBusDiagnosticsTraceParent)]
        [TestCase(Headers.DiagnosticsTraceParent)] // for backwards compatibility
        public void Should_attach_to_header_trace_when_available_and_no_ambient_activity(string headerName)
        {
            var sendActivity = CreateCompletedActivity("send activity");

            var messageHeaders = new Dictionary<string, string> { { headerName, sendActivity.Id! } };

            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext(messageHeaders));

            Assert.That(activity, Is.Not.Null, "should create activity for receive pipeline");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(activity.ParentId, Is.EqualTo(sendActivity.Id));
                Assert.That(activity.Links.Count(), Is.EqualTo(0), "should not link to logical send span");
            }
        }

        [Test]
        public void Should_attach_to_ambient_activity_when_available_and_link_to_header_trace()
        {
            var sendActivity = CreateCompletedActivity("send activity");

            using var ambientActivity = new Activity("transport sdk receive activity");
            ambientActivity.Start();

            var messageHeaders = new Dictionary<string, string> { { Headers.NServiceBusDiagnosticsTraceParent, sendActivity.Id! } };

            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext(messageHeaders));

            Assert.That(activity, Is.Not.Null, "should create activity for receive pipeline");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(activity.ParentId, Is.EqualTo(ambientActivity.Id), "should use the ambient transport activity as parent");
                Assert.That(activity.Links.Count(), Is.EqualTo(1), "should link to logical send span");
                Assert.That(activity.Links.Single().Context.TraceId, Is.EqualTo(sendActivity.TraceId));
                Assert.That(activity.Links.Single().Context.SpanId, Is.EqualTo(sendActivity.SpanId));
            }
        }

        [Test]
        public void Should_prefer_nservicebus_trace_header_over_w3c_trace_header()
        {
            var sendActivity = CreateCompletedActivity("send activity");
            var transportActivity = CreateCompletedActivity("transport activity that overwrote the w3c header");

            var messageHeaders = new Dictionary<string, string>
            {
                { Headers.NServiceBusDiagnosticsTraceParent, sendActivity.Id! },
                { Headers.DiagnosticsTraceParent, transportActivity.Id! }
            };

            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext(messageHeaders));

            Assert.That(activity, Is.Not.Null, "should create activity for receive pipeline");
            Assert.That(activity.ParentId, Is.EqualTo(sendActivity.Id), "should use the NServiceBus header, not the W3C one");
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
        public void Should_propagate_header_trace_state_when_attached_to_header_trace()
        {
            var sendActivity = CreateCompletedActivity("send activity");

            var messageHeaders = new Dictionary<string, string>
            {
                { Headers.NServiceBusDiagnosticsTraceParent, sendActivity.Id! },
                { Headers.DiagnosticsTraceState, "vendor=value" }
            };

            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext(messageHeaders));

            Assert.That(activity, Is.Not.Null, "should create activity for receive pipeline");
            Assert.That(activity.TraceStateString, Is.EqualTo("vendor=value"));
        }

        [Test]
        public void Should_propagate_header_trace_state_when_attached_to_ambient_activity()
        {
            var sendActivity = CreateCompletedActivity("send activity");

            using var ambientActivity = new Activity("transport sdk receive activity");
            ambientActivity.Start();

            var messageHeaders = new Dictionary<string, string>
            {
                { Headers.NServiceBusDiagnosticsTraceParent, sendActivity.Id! },
                { Headers.DiagnosticsTraceState, "vendor=value" }
            };

            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext(messageHeaders));

            Assert.That(activity, Is.Not.Null, "should create activity for receive pipeline");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(activity.Parent, Is.SameAs(ambientActivity));
                Assert.That(activity.TraceStateString, Is.EqualTo("vendor=value"));
            }
        }

        [Test]
        public void Should_not_propagate_header_trace_state_when_starting_new_trace()
        {
            var sendActivity = CreateCompletedActivity("send activity");

            var messageHeaders = new Dictionary<string, string>
            {
                { Headers.NServiceBusDiagnosticsTraceParent, sendActivity.Id! },
                { Headers.StartNewTrace, bool.TrueString },
                { Headers.DiagnosticsTraceState, "vendor=value" }
            };

            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext(messageHeaders));

            Assert.That(activity, Is.Not.Null, "should create activity for receive pipeline");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(activity.Parent, Is.Null, "a new trace has no parent");
                Assert.That(activity.TraceStateString, Is.Null, "trace state belongs to the trace it was recorded in");
            }
        }

        [Test]
        public void Should_return_started_activity()
        {
            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext());

            Assert.That(activity, Is.Not.Null, "should create activity for receive pipeline");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(activity.Id, Is.Not.Null, "an id is only assigned to a started activity");
                Assert.That(Activity.Current, Is.SameAs(activity));
            }
        }

        [Test]
        public void Should_propagate_header_baggage_when_attached_to_ambient_activity()
        {
            var sendActivity = CreateCompletedActivity("send activity");

            using var ambientActivity = new Activity("transport sdk receive activity");
            ambientActivity.Start();

            var messageHeaders = new Dictionary<string, string>
            {
                { Headers.NServiceBusDiagnosticsTraceParent, sendActivity.Id! },
                { Headers.DiagnosticsBaggage, "tenant=acme" }
            };

            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext(messageHeaders));

            Assert.That(activity, Is.Not.Null, "should create activity for receive pipeline");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(activity.Parent, Is.SameAs(ambientActivity));
                Assert.That(activity.GetBaggageItem("tenant"), Is.EqualTo("acme"), "transport SDKs do not propagate baggage, so NServiceBus propagates it even with an ambient parent");
            }
        }

        [Test]
        public void Should_not_add_header_baggage_already_carried_by_ambient_parent()
        {
            var sendActivity = CreateCompletedActivity("send activity");

            using var ambientActivity = new Activity("transport sdk receive activity that extracted baggage");
            ambientActivity.AddBaggage("tenant", "acme");
            ambientActivity.Start();

            var messageHeaders = new Dictionary<string, string>
            {
                { Headers.NServiceBusDiagnosticsTraceParent, sendActivity.Id! },
                { Headers.DiagnosticsBaggage, "tenant=acme,region=eu" }
            };

            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext(messageHeaders));

            Assert.That(activity, Is.Not.Null, "should create activity for receive pipeline");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(activity.Parent, Is.SameAs(ambientActivity));
                Assert.That(activity.GetBaggageItem("tenant"), Is.EqualTo("acme"));
                Assert.That(activity.GetBaggageItem("region"), Is.EqualTo("eu"));
                Assert.That(activity.Baggage.Count(item => item.Key == "tenant"), Is.EqualTo(1), "a key inherited from the parent must not be added again or it doubles on every hop");
            }
        }

        [Test]
        public void Should_propagate_header_baggage_but_not_ambient_baggage_when_starting_new_trace()
        {
            var sendActivity = CreateCompletedActivity("send activity");

            using var ambientActivity = new Activity("transport sdk receive activity");
            ambientActivity.AddBaggage("ambient-only", "value");
            ambientActivity.Start();

            var messageHeaders = new Dictionary<string, string>
            {
                { Headers.NServiceBusDiagnosticsTraceParent, sendActivity.Id! },
                { Headers.StartNewTrace, bool.TrueString },
                { Headers.DiagnosticsBaggage, "tenant=acme" }
            };

            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext(messageHeaders));

            Assert.That(activity, Is.Not.Null, "should create activity for receive pipeline");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(activity.Parent, Is.Null, "a new trace has no parent");
                Assert.That(activity.GetBaggageItem("tenant"), Is.EqualTo("acme"), "baggage from the message is propagated regardless of the trace shape");
                Assert.That(activity.GetBaggageItem("ambient-only"), Is.Null, "the ambient activity is not part of the new trace");
            }
        }

        [Test]
        public void Should_ignore_baggage_header_when_no_trace_header()
        {
            var messageHeaders = new Dictionary<string, string> { { Headers.DiagnosticsBaggage, "tenant=acme" } };

            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext(messageHeaders));

            Assert.That(activity, Is.Not.Null, "should create activity for receive pipeline");
            Assert.That(activity.GetBaggageItem("tenant"), Is.Null, "baggage is only meaningful together with a trace parent");
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

        static MessageContext CreateMessageContext(Dictionary<string, string>? messageHeaders = null) =>
            new(
                Guid.NewGuid().ToString(),
                messageHeaders ?? [],
                Array.Empty<byte>(),
                new TransportTransaction(),
                "receiver",
                new ContextBag());
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

    class PropagationDataSampling
    {
        readonly ActivityFactory activityFactory = new(new InstrumentationOptions());

        TestingActivityListener nsbActivityListener;

        [OneTimeSetUp]
        public void Setup() => nsbActivityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener(ActivitySamplingResult.PropagationData);

        [OneTimeTearDown]
        public void TearDown() => nsbActivityListener.Dispose();

        [Test]
        public void Should_create_activity_but_skip_header_tag_promotion()
        {
            var messageHeaders = new Dictionary<string, string> { { Headers.SagaId, Guid.NewGuid().ToString() } };
            var messageContext = CreateMessageContext(messageHeaders);

            var activity = activityFactory.StartIncomingPipelineActivity(messageContext);

            Assert.That(activity, Is.Not.Null, "PropagationData should still create an activity");
            var tags = activity.Tags.ToImmutableDictionary();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(activity.IsAllDataRequested, Is.False);
                Assert.That(tags.ContainsKey(ActivityTags.SagaId), Is.False, "should not promote headers to tags when not all data requested");
                Assert.That(tags[ActivityTags.NativeMessageId], Is.EqualTo(messageContext.NativeMessageId), "native message id tag should always be set");
            }
        }

        [Test]
        public void Should_not_add_exception_event_or_legacy_tags_on_error()
        {
            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext());
            Assert.That(activity, Is.Not.Null);

            activityFactory.RecordError(activity, new Exception("boom"), new ServiceCollection().BuildServiceProvider());

            var tags = activity.Tags.ToImmutableDictionary();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Error), "status should always be set");
                Assert.That(tags.ContainsKey(ActivityTags.ErrorType), Is.True, "error type tag should always be set");
                Assert.That(tags.ContainsKey("otel.status_code"), Is.False, "legacy tags should be skipped when not all data requested");
                Assert.That(activity.Events, Is.Empty, "no exception event should be recorded when not all data requested");
            }
        }

        [Test]
        public void Should_not_update_activity_from_recoverability_action()
        {
            var activity = activityFactory.StartIncomingPipelineActivity(CreateMessageContext());
            Assert.That(activity, Is.Not.Null);
            var originalDisplayName = activity.DisplayName;

            activityFactory.UpdateActivityFromRecoverabilityAction(activity, new ImmediateRetry(), "receiveAddress");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(activity.DisplayName, Is.EqualTo(originalDisplayName), "display name should not change when not all data requested");
                Assert.That(activity.Tags.ToImmutableDictionary().ContainsKey(ActivityTags.RecoverabilityAction), Is.False);
            }
        }

        static MessageContext CreateMessageContext(Dictionary<string, string>? messageHeaders = null) =>
            new(
                Guid.NewGuid().ToString(),
                messageHeaders ?? [],
                Array.Empty<byte>(),
                new TransportTransaction(),
                "receiver",
                new ContextBag());
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

    class RecordError : ActivityFactoryTests
    {
        [Test]
        public void Should_set_legacy_tags_on_every_activity_even_after_exception_already_recorded()
        {
            // Simulates an exception unwinding through the pipeline: the handler activity records it first,
            // then the same exception instance reaches RecordError again on the enclosing pipeline activity.
            using var pipelineActivity = ActivitySources.Main.StartActivity("pipeline activity");
            Assert.That(pipelineActivity, Is.Not.Null);
            using var handlerActivity = ActivitySources.Main.StartActivity("handler activity");
            Assert.That(handlerActivity, Is.Not.Null);
            var exception = new Exception("boom");

            activityFactory.RecordError(handlerActivity!, exception, new ServiceCollection().BuildServiceProvider());
            activityFactory.RecordError(pipelineActivity!, exception, new ServiceCollection().BuildServiceProvider());

            var pipelineTags = pipelineActivity!.Tags.ToImmutableDictionary();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(pipelineActivity.Status, Is.EqualTo(ActivityStatusCode.Error), "status should be set on every activity");
                Assert.That(pipelineTags.ContainsKey("otel.status_code"), Is.True, "legacy tags should be set on every activity, not deduped via ExceptionRecordedFlag");
                Assert.That(pipelineTags.ContainsKey("otel.status_description"), Is.True, "legacy tags should be set on every activity, not deduped via ExceptionRecordedFlag");
                Assert.That(handlerActivity!.Events.Count(), Is.EqualTo(1), "the first activity to see the exception records the event");
                Assert.That(pipelineActivity.Events, Is.Empty, "the exception event itself should still only be recorded once");
            }
        }
    }
}