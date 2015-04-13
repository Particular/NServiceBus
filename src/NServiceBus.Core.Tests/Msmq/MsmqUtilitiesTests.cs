namespace NServiceBus.Core.Tests.Msmq
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Messaging;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class MsmqUtilitiesTests
    {
        [Test]
        public void Should_convert_a_message_back_even_if_special_characters_are_contained_in_the_headers()
        {
            var expected = String.Format("Can u see this '{0}' character!", (char)0x19);
            
            var options = new TransportSendOptions("destination");


            var message = MsmqUtilities.Convert(new OutgoingMessage("message id",new Dictionary<string, string> { { "NServiceBus.ExceptionInfo.Message" ,expected} }, new byte[0]), options);
            var headers = MsmqUtilities.ExtractHeaders(message);

            Assert.AreEqual(expected, headers["NServiceBus.ExceptionInfo.Message"]);
        }

        [Test]
        public void Should_convert_message_headers_that_contain_nulls_at_the_end()
        {
            var expected = "Hello World!";
            var options = new TransportSendOptions("destination");

            Console.Out.WriteLine(sizeof(char));
            var message = MsmqUtilities.Convert(new OutgoingMessage("message id",new Dictionary<string, string> { { "NServiceBus.ExceptionInfo.Message", expected } }, new byte[0]), options);
            var bufferWithNulls = new byte[message.Extension.Length + (10 * sizeof(char))];
            
            Buffer.BlockCopy(message.Extension, 0, bufferWithNulls, 0, bufferWithNulls.Length - (10 * sizeof(char)));
            
            message.Extension = bufferWithNulls;

            var headers = MsmqUtilities.ExtractHeaders(message);

            Assert.AreEqual(expected, headers["NServiceBus.ExceptionInfo.Message"]);
        }

        [Test]
        public void Should_fetch_the_replytoaddress_from_responsequeue_for_backwards_compatibility()
        {
            var message = MsmqUtilities.Convert(new OutgoingMessage("message id", new Dictionary<string, string>(), new byte[0]), new TransportSendOptions("destination"));

            message.ResponseQueue = new MessageQueue(MsmqUtilities.GetReturnAddress("local", Environment.MachineName));
            var headers = MsmqUtilities.ExtractHeaders(message);

            Assert.AreEqual("local@"+Environment.MachineName, headers[Headers.ReplyToAddress]);
        }

        [Test]
        public void Should_use_the_TTBR_in_the_send_options_if_set()
        {
            var options = new TransportSendOptions("destination", TimeSpan.FromDays(1));

            var message = MsmqUtilities.Convert(new OutgoingMessage("message id",new Dictionary<string, string>(),  new byte[0]), options);

            Assert.AreEqual(options.TimeToBeReceived.Value, message.TimeToBeReceived);
        }


        [Test]
        public void Should_use_the_non_durable_setting()
        {
            var options = new TransportSendOptions("destination", nonDurable:true);

      
            Assert.False(MsmqUtilities.Convert(new OutgoingMessage("message id", new Dictionary<string, string>(), new byte[0]), options).Recoverable);
            Assert.True(MsmqUtilities.Convert(new OutgoingMessage("message id", new Dictionary<string, string>(), new byte[0]), new TransportSendOptions("destination")).Recoverable);
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