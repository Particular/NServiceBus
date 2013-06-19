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
    }
}
