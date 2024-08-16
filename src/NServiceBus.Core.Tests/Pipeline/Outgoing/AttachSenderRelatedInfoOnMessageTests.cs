namespace NServiceBus.Core.Tests.Pipeline.Outgoing;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Routing;
using Transport;
using NUnit.Framework;
using Testing;
using System.Threading;
using DelayedDelivery;
using Extensibility;

[TestFixture]
public class AttachSenderRelatedInfoOnMessageTests
{
    [Test]
    public async Task Should_set_the_time_sent_headerAsync()
    {
        var message = await InvokeBehaviorAsync();

        Assert.That(message.Headers.ContainsKey(Headers.TimeSent), Is.True);
    }

    [Test]
    public async Task Should_not_override_the_time_sent_headerAsync()
    {
        var timeSent = DateTime.UtcNow.ToString();

        var message = await InvokeBehaviorAsync(new Dictionary<string, string>
        {
            {Headers.TimeSent, timeSent}
        });

        Assert.That(message.Headers.ContainsKey(Headers.TimeSent), Is.True);
        Assert.That(message.Headers[Headers.TimeSent], Is.EqualTo(timeSent));
    }

    [Test]
    public async Task Should_set_the_nsb_version_headerAsync()
    {
        var message = await InvokeBehaviorAsync();

        Assert.That(message.Headers.ContainsKey(Headers.NServiceBusVersion), Is.True);
    }

    [Test]
    public async Task Should_not_override_nsb_version_headerAsync()
    {
        var nsbVersion = "some-crazy-version-number";
        var message = await InvokeBehaviorAsync(new Dictionary<string, string>
        {
             {Headers.NServiceBusVersion, nsbVersion}
        });

        Assert.That(message.Headers.ContainsKey(Headers.NServiceBusVersion), Is.True);
        Assert.That(message.Headers[Headers.NServiceBusVersion], Is.EqualTo(nsbVersion));
    }

    [Test]
    public async Task Should_set_deliver_At_header_when_delay_delivery_with_setAsync()
    {
        var message = await InvokeBehaviorAsync(null, new DispatchProperties
        {
            DelayDeliveryWith = new DelayDeliveryWith(TimeSpan.FromSeconds(2))
        });

        Assert.That(message.Headers.ContainsKey(Headers.DeliverAt), Is.True);
    }

    [Test]
    public async Task Should_set_deliver_at_header_when_do_not_deliver_before_is_setAsync()
    {
        var doNotDeliverBefore = DateTimeOffset.UtcNow;
        var message = await InvokeBehaviorAsync(null, new DispatchProperties
        {
            DoNotDeliverBefore = new DoNotDeliverBefore(doNotDeliverBefore)
        });

        Assert.That(message.Headers.ContainsKey(Headers.DeliverAt), Is.True);
        Assert.That(message.Headers[Headers.DeliverAt], Is.EqualTo(DateTimeOffsetHelper.ToWireFormattedString(doNotDeliverBefore)));
    }

    [Test]
    public async Task Should_not_override_deliver_at_headerAsync()
    {
        var doNotDeliverBefore = DateTimeOffset.UtcNow;
        var message = await InvokeBehaviorAsync(new Dictionary<string, string>
        {
            {Headers.DeliverAt, DateTimeOffsetHelper.ToWireFormattedString(doNotDeliverBefore)}
        }, new DispatchProperties
        {
            DelayDeliveryWith = new DelayDeliveryWith(TimeSpan.FromSeconds(2))
        });

        Assert.That(message.Headers.ContainsKey(Headers.DeliverAt), Is.True);
        Assert.That(message.Headers[Headers.DeliverAt], Is.EqualTo(DateTimeOffsetHelper.ToWireFormattedString(doNotDeliverBefore)));
    }

    static async Task<OutgoingMessage> InvokeBehaviorAsync(Dictionary<string, string> headers = null, DispatchProperties dispatchProperties = null, CancellationToken cancellationToken = default)
    {
        var message = new OutgoingMessage("id", headers ?? [], null);
        var stash = new ContextBag();

        if (dispatchProperties != null)
        {
            stash.Set(dispatchProperties);
        }

        await new AttachSenderRelatedInfoOnMessageBehavior()
            .Invoke(new TestableRoutingContext { Message = message, Extensions = stash, RoutingStrategies = new List<UnicastRoutingStrategy> { new UnicastRoutingStrategy("_") }, CancellationToken = cancellationToken }, _ => Task.CompletedTask);

        return message;
    }
}