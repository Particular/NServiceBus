namespace NServiceBus.Gateway.Tests
{
    using NUnit.Framework;
    using Rhino.Mocks;
    using Unicast.Transport;

    [TestFixture]
    public class When_the_gateway_receives_a_message:on_its_input_queue
    {
        [Test]
        public void Should_forward_the_message_onto_the_configured_channel()
        {
        
            SendMessageToGatewayQueue(new RegularMessage
                                          {
                                              SomeProperty = "test"
                                          });

            notifier.AssertWasCalled(x => x.RaiseMessageProcessed(Arg<TransportTypeEnum>.Is.Equal(TransportTypeEnum.FromHttpToMsmq),
                                                                  Arg<TransportMessage>.Is.NotNull));

        }
    }


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

            notifier.AssertWasCalled(x => x.RaiseMessageProcessed(Arg<TransportTypeEnum>.Is.Equal(TransportTypeEnum.FromHttpToMsmq),
                                                                  Arg<TransportMessage>.Is.NotNull));

        }
    }
    public class RegularMessage : IMessage
    {
        public string SomeProperty{ get; set; }
    }

    public class MessageWithADataBusProperty : IMessage
    {
        public DataBusProperty<string> LargeString { get; set; }
    }
}