namespace NServiceBus.Core.Tests.Recoverability;

using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.Routing;
using NServiceBus.Transport;
using NUnit.Framework;
using Testing;

[TestFixture]
public class DelayedRetryRecoverabilityActionTests
{
    [Test]
    public void When_delay_message_retry()
    {
        var recoverabilityContext = CreateRecoverabilityContext();
        var delay = TimeSpan.FromSeconds(42);
        var delayedRetryAction = new DelayedRetry(delay);

        var routingContext = delayedRetryAction.GetRoutingContexts(recoverabilityContext)
            .Single();

        var routingStrategy = routingContext.RoutingStrategies.Single() as UnicastRoutingStrategy;

        Assert.Multiple(() =>
        {
            Assert.That((routingStrategy.Apply([]) as UnicastAddressTag).Destination, Is.EqualTo(recoverabilityContext.ReceiveAddress));
            Assert.That(routingContext.Extensions.Get<DispatchProperties>().DelayDeliveryWith.Delay, Is.EqualTo(delay));
            Assert.That(delayedRetryAction.ErrorHandleResult, Is.EqualTo(ErrorHandleResult.Handled));
        });
    }

    [Test]
    public void Should_update_retry_headers_when_present()
    {
        var delayedRetryAction = new DelayedRetry(TimeSpan.Zero);
        var originalHeadersTimestamp = DateTimeOffsetHelper.ToWireFormattedString(new DateTimeOffset(2012, 12, 12, 0, 0, 0, TimeSpan.Zero));
        var delayedDeliveriesPerformed = 2;
        var recoverabilityContext = CreateRecoverabilityContext(new Dictionary<string, string>
        {
            {Headers.DelayedRetriesTimestamp, originalHeadersTimestamp},
            {Headers.DelayedRetries, delayedDeliveriesPerformed.ToString()}
        }, delayedDeliveriesPerformed: delayedDeliveriesPerformed);

        var now = DateTimeOffset.UtcNow;
        var routingContexts = delayedRetryAction.GetRoutingContexts(recoverabilityContext);

        var incomingMessage = recoverabilityContext.FailedMessage;

        var outgoingMessageHeaders = routingContexts.Single().Message.Headers;

        Assert.Multiple(() =>
        {
            Assert.That(outgoingMessageHeaders[Headers.DelayedRetries], Is.EqualTo("3"));
            Assert.That(incomingMessage.Headers[Headers.DelayedRetries], Is.EqualTo(delayedDeliveriesPerformed.ToString()));
        });

        var utcDateTime = DateTimeOffsetHelper.ToDateTimeOffset(outgoingMessageHeaders[Headers.DelayedRetriesTimestamp]);
        // the serialization removes precision which may lead to now being greater than the deserialized header value
        var adjustedNow = DateTimeOffsetHelper.ToDateTimeOffset(DateTimeOffsetHelper.ToWireFormattedString(now));
        Assert.Multiple(() =>
        {
            Assert.That(utcDateTime, Is.GreaterThanOrEqualTo(adjustedNow));
            Assert.That(incomingMessage.Headers[Headers.DelayedRetriesTimestamp], Is.EqualTo(originalHeadersTimestamp));
        });
    }

    [Test]
    public void Should_add_retry_headers_when_not_present()
    {
        var delayedRetryAction = new DelayedRetry(TimeSpan.Zero);
        var recoverabilityContext = CreateRecoverabilityContext();

        var routingContexts = delayedRetryAction.GetRoutingContexts(recoverabilityContext);

        var outgoingMessageHeaders = routingContexts.Single().Message.Headers;

        Assert.Multiple(() =>
        {
            Assert.That(outgoingMessageHeaders[Headers.DelayedRetries], Is.EqualTo("1"));
            Assert.That(recoverabilityContext.FailedMessage.Headers.ContainsKey(Headers.DelayedRetries), Is.False);
            Assert.That(outgoingMessageHeaders.ContainsKey(Headers.DelayedRetriesTimestamp), Is.True);
            Assert.That(recoverabilityContext.FailedMessage.Headers.ContainsKey(Headers.DelayedRetriesTimestamp), Is.False);
        });
    }

    static TestableRecoverabilityContext CreateRecoverabilityContext(Dictionary<string, string> headers = null, int delayedDeliveriesPerformed = 0)
    {
        return new TestableRecoverabilityContext
        {
            FailedMessage = new IncomingMessage("messageId", headers ?? [], ReadOnlyMemory<byte>.Empty),
            DelayedDeliveriesPerformed = delayedDeliveriesPerformed
        };
    }
}