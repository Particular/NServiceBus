namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using NUnit.Framework;
    using Transport;

    public class When_delivery_constraints_contain_multiple_delayed_constraints : NServiceBusTransportTest
    {
        [Test]
        public async Task Should_throw_exception()
        {
            await StartPump(c =>
                {
                    Assert.Fail("message should not be sent");
                    return Task.FromResult(0);
                }, e => Task.FromResult(ErrorHandleResult.Handled),
                TransportTransactionMode.None);

            var invalidDeliveryConstraints = new List<DeliveryConstraint>();
            invalidDeliveryConstraints.Add(new DelayDeliveryWith(TimeSpan.FromHours(1)));
            invalidDeliveryConstraints.Add(new DoNotDeliverBefore(DateTime.Now.AddDays(1)));

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await SendMessage(InputQueueName, deliveryConstraints: invalidDeliveryConstraints));
        }
    }
}