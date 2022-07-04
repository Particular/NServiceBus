namespace NServiceBus.Core.Tests.Diagnostics;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Extensibility;
using Helpers;
using NUnit.Framework;
using Transport;

[TestFixture]
public class ActivityFactoryTests
{
    class StartIncomingActivityTests
    {
        TestingActivityListener nsbActivityListener;
        ActivityFactory activityFactory = new();


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
}