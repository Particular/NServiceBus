namespace NServiceBus.Core.Tests.DataBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;
    using Rhino.Mocks;
    using Unicast;
    using Unicast.Messages;

    [TestFixture]
    class When_applying_the_databus_message_mutator_to_outgoing_messages : on_the_bus
    {
        [Test]
        public void Outgoing_databus_properties_should_be_dehydrated()
        {
            var metadata = new MessageMetadata();
            var message = new LogicalMessage(metadata, new MessageWithDataBusProperty
            {
                DataBusProperty = new DataBusProperty<string>("test")
            }, new Dictionary<string, string>(), null);

            Invoke(message);

            dataBus.AssertWasCalled(
                x => x.Put(Arg<Stream>.Is.Anything, Arg<TimeSpan>.Is.Equal(TimeSpan.MaxValue)));
        }

        void Invoke(LogicalMessage message)
        {
           
            var context = new OutgoingContext(null, new SendOptions(Address.Parse("MyEndpoint")), message);

            sendBehavior.Invoke(context, () => { });
        }

        [Test]
        public void Time_to_live_should_be_passed_on_the_databus()
        {
            var metadata = new MessageMetadata(timeToBeReceived: TimeSpan.FromMinutes(1));
            var message = new LogicalMessage(metadata, new MessageWithExplicitTimeToLive
            {
                DataBusProperty = new DataBusProperty<string>("test")
            }, new Dictionary<string, string>(), null);

            Invoke(message);
           
            dataBus.AssertWasCalled(
                x => x.Put(Arg<Stream>.Is.Anything, Arg<TimeSpan>.Is.Equal(TimeSpan.FromMinutes(1))));
        }
    }
}