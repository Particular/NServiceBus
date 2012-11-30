namespace NServiceBus.Core.Tests.DataBus
{
    using System;
    using System.IO;
    using NUnit.Framework;
    using Rhino.Mocks;

    [TestFixture]
    public class When_applying_the_databus_message_mutator_to_outgoing_messages : on_the_bus
    {
        [Test]
        public void Outgoing_databus_properties_should_be_dehydrated()
        {

            var message = new MessageWithDataBusProperty
                              {
                                  DataBusProperty = new DataBusProperty<string>("test")
                              };


            outgoingMutator.MutateOutgoing(message);

            dataBus.AssertWasCalled(
                x => x.Put(Arg<Stream>.Is.Anything, Arg<TimeSpan>.Is.Equal(TimeSpan.MaxValue)));
        }

        [Test]
        public void Time_to_live_should_be_passed_on_the_databus()
        {

            var message = new MessageWithExplicitTimeToLive()
                              {
                                  DataBusProperty = new DataBusProperty<string>("test")
                              };


            outgoingMutator.MutateOutgoing(message);

            dataBus.AssertWasCalled(
                x => x.Put(Arg<Stream>.Is.Anything, Arg<TimeSpan>.Is.Equal(TimeSpan.FromMinutes(1))));
        }
    }
}