namespace NServiceBus.Gateway.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class When_the_gateway_receives_a_message_with_a_databus_property : on_its_input_queue
    {
        [Test]
        public void Should_transmit_the_databus_payload_on_the_same_channel_as_the_message()
        {
            var testString = "A laaarge string";

            var message = new MessageWithADataBusProperty
                              {
                                  LargeString = new DataBusProperty<string>(testString)
                              };
            SendMessageToGatewayQueue(message);

            var propertyKey = message.LargeString.Key;

            var transportMessage = GetResultingMessage();

            string dataBusKey = null;
            
            transportMessage.Headers.TryGetValue("NServiceBus.DataBus." + propertyKey, out dataBusKey);

            //make sure that we got the key
            Assert.NotNull(dataBusKey);

            //make sure that they key exist in our databus
            Assert.NotNull(dataBusForTheReceivingSide.Get(dataBusKey));
        }
    }

    public class MessageWithADataBusProperty : IMessage
    {
        public DataBusProperty<string> LargeString { get; set; }
    }
}