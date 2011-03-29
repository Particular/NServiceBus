namespace NServiceBus.Gateway.Tests
{
    using NUnit.Framework;

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


            Assert.NotNull(GetResultingMessage());
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

            Assert.NotNull(GetResultingMessage());
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