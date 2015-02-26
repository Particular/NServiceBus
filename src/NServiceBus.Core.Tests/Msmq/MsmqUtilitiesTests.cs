namespace NServiceBus.Core.Tests.Msmq
{
    using System;
    using System.Linq;
    using System.Messaging;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using NUnit.Framework;

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

        [Test]
        public void Should_fetch_the_replytoaddress_from_responsequeue_for_backwards_compatibility()
        {
            var transportMessage = new TransportMessage();
            var message = MsmqUtilities.Convert(transportMessage);

            message.ResponseQueue = new MessageQueue(MsmqUtilities.GetReturnAddress("local", Environment.MachineName));
            var result = MsmqUtilities.Convert(message);

            Assert.AreEqual("local@"+Environment.MachineName, result.Headers[Headers.ReplyToAddress]);
        }

  

        [Test]
        public void GetReturnAddress_for_both_with_machine_name_should_return_replyToAddress_with_reply_machine()
        {
            var returnAddress = MsmqUtilities.GetReturnAddress("replytoaddress@replytomachine", "detinationmachine");
            Assert.AreEqual(@"FormatName:DIRECT=OS:replytomachine\private$\replytoaddress", returnAddress);
        }

        [Test]
        public void GetReturnAddress_for_both_with_ipAddress_should_return_replyToAddress_with_localIP()
        {
            var returnAddress = MsmqUtilities.GetReturnAddress("replytoaddress@202.171.13.141", "202.171.13.140");
            returnAddress = returnAddress.Replace(LocalIPAddress(), "TheLocalIP");
            Assert.AreEqual(@"FormatName:DIRECT=TCP:TheLocalIP\private$\replytoaddress", returnAddress);
        }

        [Test]
        public void GetReturnAddress_for_destination_with_ipAddress_should_return_replyToAddress_with_localIP()
        {
            var returnAddress = MsmqUtilities.GetReturnAddress("replytoaddress@replytomachine", "202.171.13.140");
            returnAddress = returnAddress.Replace(LocalIPAddress(), "TheLocalIP");
            Assert.AreEqual(@"FormatName:DIRECT=TCP:TheLocalIP\private$\replytoaddress", returnAddress);
        }

        string LocalIPAddress()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            var host = Dns.GetHostEntry(Dns.GetHostName());

            return host
                .AddressList
                .First(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .ToString();
        }
    }
}
