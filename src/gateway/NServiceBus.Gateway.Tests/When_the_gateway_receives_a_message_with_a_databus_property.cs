namespace NServiceBus.Gateway.Tests
{
    using System;
    using NUnit.Framework;
    using Unicast.Transport;

    [TestFixture]
    public class When_the_gateway_receives_a_message_with_a_databus_property : on_its_input_queue
    {
        [Test]
        public void Should_transmit_the_databus_payload_on_the_same_channel_as_the_message()
        {
            SendMessageToGatewayQueue(new MessageWithADataBusProperty
                                          {
                                              LargeString = new DataBusProperty<string>("a laaaarge string")
                                          });

            Assert.NotNull(GetResultingMessage());
        }
    }
}