namespace NServiceBus.Core.Tests.Msmq
{
    using System;
    using NUnit.Framework;
    using Transports.Msmq;

    [TestFixture]
    public class MsmqUtilitiesTests
    {
        [Test]
        public void Should_convert_a_message_back_even_if_special_characters_are_contained_in_the_headers()
        {
            var expected = String.Format("Can u see this '{0}' character!", (char)0x19);
            var transportMessage = new TransportMessage();
            transportMessage.Headers.Add("NServiceBus.ExceptionInfo.Message", expected);
            
            var message = MsmqUtilities.Convert(transportMessage);
            var result = MsmqUtilities.Convert(message);

            Assert.AreEqual(expected, result.Headers["NServiceBus.ExceptionInfo.Message"]);
        }

        [Test]
        public void Should_convert_message_headers_that_contain_nulls_at_the_end()
        {
            var expected = "Hello World!";
            var transportMessage = new TransportMessage();
            transportMessage.Headers.Add("NServiceBus.ExceptionInfo.Message", expected);

            Console.Out.WriteLine(sizeof(char));
            var message = MsmqUtilities.Convert(transportMessage);
            var bufferWithNulls = new byte[message.Extension.Length + (10 * sizeof(char))];
            
            Buffer.BlockCopy(message.Extension, 0, bufferWithNulls, 0, bufferWithNulls.Length - (10 * sizeof(char)));
            
            message.Extension = bufferWithNulls;

            var result = MsmqUtilities.Convert(message);

            Assert.AreEqual(expected, result.Headers["NServiceBus.ExceptionInfo.Message"]);
        }
    }
}
