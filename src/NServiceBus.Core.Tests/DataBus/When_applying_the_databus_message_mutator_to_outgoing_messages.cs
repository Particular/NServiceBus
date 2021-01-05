using NServiceBus.Transport;

namespace NServiceBus.Core.Tests.DataBus
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using NServiceBus.DataBus;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    class When_applying_the_databus_message_mutator_to_outgoing_messages
    {
        [Test]
        public async Task Outgoing_databus_properties_should_be_dehydrated()
        {
            var context = new TestableOutgoingLogicalMessageContext();
            context.Message = new OutgoingLogicalMessage(typeof(MessageWithDataBusProperty), new MessageWithDataBusProperty
            {
                DataBusProperty = new DataBusProperty<string>("test")
            });

            var fakeDatabus = new FakeDataBus();

            var sendBehavior = new DataBusSendBehavior(fakeDatabus, new DefaultDataBusSerializer(), new Conventions());

            await sendBehavior.Invoke(context, ctx => Task.CompletedTask);

            Assert.AreEqual(TimeSpan.MaxValue, fakeDatabus.TTBRUsed);
        }

        [Test]
        public async Task Time_to_live_should_be_passed_on_the_databus()
        {
            var context = new TestableOutgoingLogicalMessageContext();
            context.Message = new OutgoingLogicalMessage(typeof(MessageWithExplicitTimeToLive), new MessageWithExplicitTimeToLive
            {
                DataBusProperty = new DataBusProperty<string>("test")
            });

            context.Extensions.GetOrCreate<OperationProperties>().DiscardIfNotReceivedBefore = new DiscardIfNotReceivedBefore(TimeSpan.FromMinutes(1));

            var fakeDatabus = new FakeDataBus();

            var sendBehavior = new DataBusSendBehavior(fakeDatabus, new DefaultDataBusSerializer(), new Conventions());

            await sendBehavior.Invoke(context, ctx => Task.CompletedTask);

            Assert.AreEqual(TimeSpan.FromMinutes(1), fakeDatabus.TTBRUsed);
        }

        class FakeDataBus : IDataBus
        {
            public Task<Stream> Get(string key)
            {
                throw new NotImplementedException();
            }

            public Task<string> Put(Stream stream, TimeSpan timeToBeReceived)
            {
                TTBRUsed = timeToBeReceived;
                return Task.FromResult(Guid.NewGuid().ToString());
            }

            public Task Start()
            {
                throw new NotImplementedException();
            }

            public TimeSpan TTBRUsed;
        }
    }
}