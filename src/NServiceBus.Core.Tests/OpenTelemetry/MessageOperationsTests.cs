namespace NServiceBus.Core.Tests.OpenTelemetry
{
    using System;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Helpers;
    using NUnit.Framework;
    using Pipeline;

    [TestFixture]
    public class MessageOperationsTests
    {
        TestingActivityListener listener;

        [OneTimeSetUp]
        public void Setup()
        {
            listener = TestingActivityListener.SetupNServiceBusDiagnosticListener();
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            listener?.Dispose();
        }

        [Test]
        public async Task Send_should_create_span()
        {
            var operations = new TestableMessageOperations();
            Activity activity = null;
            operations.SendPipeline.OnInvoke = _ =>
            {
                activity = Activity.Current;
            };

            await operations.Send(new FakeRootContext(), new object(), new SendOptions());

            Assert.IsNotNull(activity);
            Assert.AreEqual(ActivityIdFormat.W3C, activity.IdFormat);
            Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
            Assert.AreEqual(activity.RootId, activity.TraceId.ToString());

            Assert.AreEqual(ActivityNames.OutgoingMessageActivityName, activity.OperationName);
            Assert.AreEqual("send message", activity.DisplayName);

            Assert.AreEqual(activity, operations.SendPipeline.LastContext.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey));
        }

        [Test]
        public async Task Publish_should_create_span()
        {
            var operations = new TestableMessageOperations();
            Activity activity = null;
            operations.PublishPipeline.OnInvoke = _ =>
            {
                activity = Activity.Current;
            };

            await operations.Publish(new FakeRootContext(), new object(), new PublishOptions());

            Assert.IsNotNull(activity);
            Assert.AreEqual(ActivityIdFormat.W3C, activity.IdFormat);
            Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
            Assert.AreEqual(activity.RootId, activity.TraceId.ToString());

            Assert.AreEqual(ActivityNames.OutgoingEventActivityName, activity.OperationName);
            Assert.AreEqual("publish event", activity.DisplayName);

            Assert.AreEqual(activity, operations.PublishPipeline.LastContext.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey));
        }

        [Test]
        public async Task Reply_should_create_span()
        {
            var operations = new TestableMessageOperations();
            Activity activity = null;
            operations.ReplyPipeline.OnInvoke = _ =>
            {
                activity = Activity.Current;
            };

            await operations.Reply(new FakeRootContext(), new object(), new ReplyOptions());

            Assert.IsNotNull(activity);
            Assert.AreEqual(ActivityIdFormat.W3C, activity.IdFormat);
            Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
            Assert.AreEqual(activity.RootId, activity.TraceId.ToString());

            Assert.AreEqual(ActivityNames.OutgoingMessageActivityName, activity.OperationName);
            Assert.AreEqual("reply", activity.DisplayName);

            Assert.AreEqual(activity, operations.ReplyPipeline.LastContext.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey));
        }

        [Test]
        public async Task Subscribe_should_create_span()
        {
            var operations = new TestableMessageOperations();
            Activity activity = null;
            operations.SubscribePipeline.OnInvoke = _ =>
            {
                activity = Activity.Current;
            };

            await operations.Subscribe(new FakeRootContext(), typeof(object), new SubscribeOptions());

            Assert.IsNotNull(activity);
            Assert.AreEqual(ActivityIdFormat.W3C, activity.IdFormat);
            Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
            Assert.AreEqual(activity.RootId, activity.TraceId.ToString());

            Assert.AreEqual(ActivityNames.SubscribeActivityName, activity.OperationName);
            Assert.AreEqual("subscribe event", activity.DisplayName);

            Assert.AreEqual(activity, operations.SubscribePipeline.LastContext.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey));
        }

        [Test]
        public async Task Unsubscribe_should_create_span()
        {
            var operations = new TestableMessageOperations();
            Activity activity = null;
            operations.UnsubscribePipeline.OnInvoke = _ =>
            {
                activity = Activity.Current;
            };

            await operations.Unsubscribe(new FakeRootContext(), typeof(object), new UnsubscribeOptions());

            Assert.IsNotNull(activity);
            Assert.AreEqual(ActivityIdFormat.W3C, activity.IdFormat);
            Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
            Assert.AreEqual(activity.RootId, activity.TraceId.ToString());

            Assert.AreEqual(ActivityNames.UnsubscribeActivityName, activity.OperationName);
            Assert.AreEqual("unsubscribe event", activity.DisplayName);

            Assert.AreEqual(activity, operations.UnsubscribePipeline.LastContext.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey));
        }

        [Test]
        public void Should_set_span_error_state_on_failure()
        {
            var operations = new TestableMessageOperations();
            Activity activity = null;
            operations.SendPipeline.OnInvoke = _ =>
            {
                activity = Activity.Current;
                throw new Exception("processing exception");
            };

            var ex = Assert.ThrowsAsync<Exception>(async () => await operations.Send(new FakeRootContext(), new object(), new SendOptions()));

            Assert.AreEqual(ActivityStatusCode.Error, activity.Status);
            var tags = activity.Tags.ToImmutableDictionary();
            Assert.AreEqual("ERROR", tags["otel.status_code"]);
            Assert.AreEqual(ex.Message, tags["otel.status_description"]);
        }

        [Test]
        public async Task Should_always_create_w3c_id_span()
        {
            var operations = new TestableMessageOperations();
            Activity activity = null;
            operations.SendPipeline.OnInvoke = _ =>
            {
                activity = Activity.Current;
            };

            using var ambientActivity = new Activity("ambient activity");
            ambientActivity.SetIdFormat(ActivityIdFormat.Hierarchical);
            ambientActivity.Start();
            Assert.AreEqual(ambientActivity.IdFormat, ActivityIdFormat.Hierarchical);

            await operations.Send(new FakeRootContext(), new object(), new SendOptions());

            Assert.IsNotNull(activity);
            Assert.AreEqual(ActivityIdFormat.W3C, activity.IdFormat);
            Assert.AreEqual(ambientActivity.Id, activity.ParentId);
            Assert.AreNotEqual(ambientActivity.TraceId, activity.TraceId);
        }
    }
}