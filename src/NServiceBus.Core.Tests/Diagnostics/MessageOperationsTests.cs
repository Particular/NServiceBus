namespace NServiceBus.Core.Tests.Diagnostics;

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using Helpers;
using MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Pipeline;
using NUnit.Framework;

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
        var sendPipeline = new FakePipeline<IOutgoingSendContext>();
        var operations = CreateMessageOperations(sendPipeline: sendPipeline);

        await operations.Send(new FakeRootContext(), new object(), new SendOptions());

        var activity = sendPipeline.CurrentActivity;
        Assert.IsNotNull(activity);
        Assert.AreEqual(ActivityIdFormat.W3C, activity.IdFormat);
        Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
        Assert.AreEqual(activity.RootId, activity.TraceId.ToString());

        Assert.AreEqual(ActivityNames.OutgoingMessageActivityName, activity.OperationName);
        Assert.AreEqual("send message", activity.DisplayName);

        //TODO: verify message intent tag == send

        Assert.AreEqual(sendPipeline.CurrentActivity, sendPipeline.Context.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey));
    }

    [Test]
    public async Task Publish_should_create_span()
    {
        var publishPipeline = new FakePipeline<IOutgoingPublishContext>();
        var operations = CreateMessageOperations(publishPipeline: publishPipeline);

        await operations.Publish(new FakeRootContext(), new object(), new PublishOptions());

        var activity = publishPipeline.CurrentActivity;
        Assert.IsNotNull(activity);
        Assert.AreEqual(ActivityIdFormat.W3C, activity.IdFormat);
        Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
        Assert.AreEqual(activity.RootId, activity.TraceId.ToString());

        Assert.AreEqual(ActivityNames.OutgoingEventActivityName, activity.OperationName);
        Assert.AreEqual("publish event", activity.DisplayName);

        //TODO: verify message intent tag == publish

        Assert.AreEqual(publishPipeline.CurrentActivity, publishPipeline.Context.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey));
    }

    [Test]
    public async Task Reply_should_create_span()
    {
        var publishPipeline = new FakePipeline<IOutgoingReplyContext>();
        var operations = CreateMessageOperations(replyPipeline: publishPipeline);

        await operations.Reply(new FakeRootContext(), new object(), new ReplyOptions());

        var activity = publishPipeline.CurrentActivity;
        Assert.IsNotNull(activity);
        Assert.AreEqual(ActivityIdFormat.W3C, activity.IdFormat);
        Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
        Assert.AreEqual(activity.RootId, activity.TraceId.ToString());

        Assert.AreEqual(ActivityNames.OutgoingMessageActivityName, activity.OperationName);
        Assert.AreEqual("reply", activity.DisplayName);

        //TODO: verify message intent tag == reply

        Assert.AreEqual(publishPipeline.CurrentActivity, publishPipeline.Context.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey));
    }

    [Test]
    public async Task Subscribe_should_create_span()
    {
        var publishPipeline = new FakePipeline<ISubscribeContext>();
        var operations = CreateMessageOperations(subscribePipeline: publishPipeline);

        await operations.Subscribe(new FakeRootContext(), typeof(object), new SubscribeOptions());

        var activity = publishPipeline.CurrentActivity;
        Assert.IsNotNull(activity);
        Assert.AreEqual(ActivityIdFormat.W3C, activity.IdFormat);
        Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
        Assert.AreEqual(activity.RootId, activity.TraceId.ToString());

        Assert.AreEqual(ActivityNames.SubscribeActivityName, activity.OperationName);
        Assert.AreEqual("subscribe event", activity.DisplayName);

        //TODO: verify message intent tag == subscribe

        Assert.AreEqual(publishPipeline.CurrentActivity, publishPipeline.Context.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey));
    }

    [Test]
    public async Task Unsubscribe_should_create_span()
    {
        var publishPipeline = new FakePipeline<IUnsubscribeContext>();
        var operations = CreateMessageOperations(unsubscribePipeline: publishPipeline);

        await operations.Unsubscribe(new FakeRootContext(), typeof(object), new UnsubscribeOptions());

        var activity = publishPipeline.CurrentActivity;
        Assert.IsNotNull(activity);
        Assert.AreEqual(ActivityIdFormat.W3C, activity.IdFormat);
        Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
        Assert.AreEqual(activity.RootId, activity.TraceId.ToString());

        Assert.AreEqual(ActivityNames.UnsubscribeActivityName, activity.OperationName);
        Assert.AreEqual("unsubscribe event", activity.DisplayName);

        //TODO: verify message intent tag == subscribe

        Assert.AreEqual(publishPipeline.CurrentActivity, publishPipeline.Context.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey));
    }

    [Test]
    public void Should_set_span_error_state_on_failure()
    {
        var sendPipeline = new FakePipeline<IOutgoingSendContext> { ShouldThrow = true };
        var operations = CreateMessageOperations(sendPipeline: sendPipeline);

        var ex = Assert.ThrowsAsync<Exception>(async () => await operations.Send(new FakeRootContext(), new object(), new SendOptions()));

        var activity = sendPipeline.CurrentActivity;
        Assert.AreEqual(ActivityStatusCode.Error, activity.Status);
        var tags = activity.Tags.ToImmutableDictionary();
        Assert.AreEqual("ERROR", tags["otel.status_code"]);
        Assert.AreEqual(ex.Message, tags["otel.status_description"]);
    }

    [Test]
    public async Task Should_always_create_w3c_id_span()
    {
        var sendPipeline = new FakePipeline<IOutgoingSendContext>();
        var operations = CreateMessageOperations(sendPipeline: sendPipeline);

        using var ambientActivity = new Activity("ambient activity");
        ambientActivity.SetIdFormat(ActivityIdFormat.Hierarchical);
        ambientActivity.Start();
        Assert.AreEqual(ambientActivity.IdFormat, ActivityIdFormat.Hierarchical);

        await operations.Send(new FakeRootContext(), new object(), new SendOptions());

        var activity = sendPipeline.CurrentActivity;
        Assert.IsNotNull(activity);
        Assert.AreEqual(ActivityIdFormat.W3C, activity.IdFormat);
        Assert.AreEqual(ambientActivity.Id, activity.ParentId);
        Assert.AreNotEqual(ambientActivity.TraceId, activity.TraceId);
    }

    MessageOperations CreateMessageOperations(
        FakePipeline<IOutgoingPublishContext> publishPipeline = null,
        FakePipeline<IOutgoingSendContext> sendPipeline = null,
        FakePipeline<IOutgoingReplyContext> replyPipeline = null,
        FakePipeline<ISubscribeContext> subscribePipeline = null,
        FakePipeline<IUnsubscribeContext> unsubscribePipeline = null)
    {
        return new MessageOperations(
            new MessageMapper(),
            publishPipeline ?? new FakePipeline<IOutgoingPublishContext>(),
            sendPipeline ?? new FakePipeline<IOutgoingSendContext>(),
            replyPipeline ?? new FakePipeline<IOutgoingReplyContext>(),
            subscribePipeline ?? new FakePipeline<ISubscribeContext>(),
            unsubscribePipeline ?? new FakePipeline<IUnsubscribeContext>(),
            new ActivityFactory());
    }

    class FakePipeline<T> : IPipeline<T> where T : IBehaviorContext
    {
        public T Context { get; set; }

        public Activity CurrentActivity { get; set; }

        public bool ShouldThrow { get; set; }

        public Task Invoke(T context)
        {
            Context = context;
            CurrentActivity = Activity.Current;

            if (ShouldThrow)
            {
                throw new Exception("processing exception");
            }

            return Task.CompletedTask;
        }
    }
}