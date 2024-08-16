namespace NServiceBus.Core.Tests.OpenTelemetry;

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
        Assert.That(activity.IdFormat, Is.EqualTo(ActivityIdFormat.W3C));
        Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Ok));
        Assert.That(activity.TraceId.ToString(), Is.EqualTo(activity.RootId));

        Assert.That(activity.OperationName, Is.EqualTo(ActivityNames.OutgoingMessageActivityName));
        Assert.That(activity.DisplayName, Is.EqualTo("send message"));

        Assert.That(operations.SendPipeline.LastContext.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey), Is.EqualTo(activity));
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
        Assert.That(activity.IdFormat, Is.EqualTo(ActivityIdFormat.W3C));
        Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Ok));
        Assert.That(activity.TraceId.ToString(), Is.EqualTo(activity.RootId));

        Assert.That(activity.OperationName, Is.EqualTo(ActivityNames.OutgoingEventActivityName));
        Assert.That(activity.DisplayName, Is.EqualTo("publish event"));

        Assert.That(operations.PublishPipeline.LastContext.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey), Is.EqualTo(activity));
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
        Assert.That(activity.IdFormat, Is.EqualTo(ActivityIdFormat.W3C));
        Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Ok));
        Assert.That(activity.TraceId.ToString(), Is.EqualTo(activity.RootId));

        Assert.That(activity.OperationName, Is.EqualTo(ActivityNames.OutgoingMessageActivityName));
        Assert.That(activity.DisplayName, Is.EqualTo("reply"));

        Assert.That(operations.ReplyPipeline.LastContext.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey), Is.EqualTo(activity));
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
        Assert.That(activity.IdFormat, Is.EqualTo(ActivityIdFormat.W3C));
        Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Ok));
        Assert.That(activity.TraceId.ToString(), Is.EqualTo(activity.RootId));

        Assert.That(activity.OperationName, Is.EqualTo(ActivityNames.SubscribeActivityName));
        Assert.That(activity.DisplayName, Is.EqualTo("subscribe event"));

        Assert.That(operations.SubscribePipeline.LastContext.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey), Is.EqualTo(activity));
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
        Assert.That(activity.IdFormat, Is.EqualTo(ActivityIdFormat.W3C));
        Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Ok));
        Assert.That(activity.TraceId.ToString(), Is.EqualTo(activity.RootId));

        Assert.That(activity.OperationName, Is.EqualTo(ActivityNames.UnsubscribeActivityName));
        Assert.That(activity.DisplayName, Is.EqualTo("unsubscribe event"));

        Assert.That(operations.UnsubscribePipeline.LastContext.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey), Is.EqualTo(activity));
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

        Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Error));
        var tags = activity.Tags.ToImmutableDictionary();
        Assert.That(tags["otel.status_code"], Is.EqualTo("ERROR"));
        Assert.That(tags["otel.status_description"], Is.EqualTo(ex.Message));
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
        Assert.That(ActivityIdFormat.Hierarchical, Is.EqualTo(ambientActivity.IdFormat));

        await operations.Send(new FakeRootContext(), new object(), new SendOptions());

        Assert.IsNotNull(activity);
        Assert.That(activity.IdFormat, Is.EqualTo(ActivityIdFormat.W3C));
        Assert.That(activity.ParentId, Is.EqualTo(ambientActivity.Id));
        Assert.AreNotEqual(ambientActivity.TraceId, activity.TraceId);
    }
}