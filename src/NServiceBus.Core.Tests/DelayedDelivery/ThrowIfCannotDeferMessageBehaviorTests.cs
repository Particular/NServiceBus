namespace NServiceBus.Core.Tests.Timeout;

using System;
using System.Threading.Tasks;
using NServiceBus.DelayedDelivery;
using NUnit.Framework;
using Testing;
using Transport;

[TestFixture]
public class ThrowIfCannotDeferMessageBehaviorTests
{
    [Test]
    public void Should_throw_when_delay_delivery_with_is_set()
    {
        var behavior = new ThrowIfCannotDeferMessageBehavior();
        var context = new TestableRoutingContext();
        context.Extensions.Set(new DispatchProperties
        {
            DelayDeliveryWith = new DelayDeliveryWith(TimeSpan.FromMinutes(1))
        });

        Assert.That(() => behavior.Invoke(context, _ => Task.CompletedTask), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void Should_throw_when_do_not_deliver_before_is_set()
    {
        var behavior = new ThrowIfCannotDeferMessageBehavior();
        var context = new TestableRoutingContext();
        context.Extensions.Set(new DispatchProperties
        {
            DoNotDeliverBefore = new DoNotDeliverBefore(DateTimeOffset.UtcNow.AddMinutes(1))
        });

        Assert.That(() => behavior.Invoke(context, _ => Task.CompletedTask), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task Should_call_next_when_dispatch_properties_exist_but_no_delay_is_requested()
    {
        var behavior = new ThrowIfCannotDeferMessageBehavior();
        var context = new TestableRoutingContext();
        context.Extensions.Set(new DispatchProperties());
        var nextCalled = false;

        await behavior.Invoke(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.That(nextCalled, Is.True);
    }

    [Test]
    public async Task Should_call_next_when_dispatch_properties_are_missing()
    {
        var behavior = new ThrowIfCannotDeferMessageBehavior();
        var context = new TestableRoutingContext();
        var nextCalled = false;

        await behavior.Invoke(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.That(nextCalled, Is.True);
    }
}
