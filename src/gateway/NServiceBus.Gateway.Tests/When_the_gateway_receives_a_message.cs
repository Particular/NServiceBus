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


    public class RegularMessage : IMessage
    {
        public string SomeProperty{ get; set; }
    }
}