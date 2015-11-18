namespace NServiceBus.Core.Tests.Msmq
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class MsmqUtilitiesTests
    {
        [Test]
        public void Should_convert_a_message_back_even_if_special_characters_are_contained_in_the_headers()
        {
            var expected = $"Can u see this '{(char) 0x19}' character.";

            var options = new DispatchOptions(new UnicastAddressTag("destination"), DispatchConsistency.Default);

            var message = MsmqUtilities.Convert(new OutgoingMessage("message id", new Dictionary<string, string>
            {
                {"NServiceBus.ExceptionInfo.Message", expected}
            }, new byte[0]), options);
            var headers = MsmqUtilities.ExtractHeaders(message);

            Assert.AreEqual(expected, headers["NServiceBus.ExceptionInfo.Message"]);
        }

        [Test]
        public void Should_convert_message_headers_that_contain_nulls_at_the_end()
        {
            var expected = "Hello World";
            var options = new DispatchOptions(new UnicastAddressTag("destination"), DispatchConsistency.Default);

            Console.Out.WriteLine(sizeof(char));
            var message = MsmqUtilities.Convert(new OutgoingMessage("message id", new Dictionary<string, string>
            {
                {"NServiceBus.ExceptionInfo.Message", expected}
            }, new byte[0]), options);
            var bufferWithNulls = new byte[message.Extension.Length + 10 * sizeof(char)];

            Buffer.BlockCopy(message.Extension, 0, bufferWithNulls, 0, bufferWithNulls.Length - 10 * sizeof(char));

            message.Extension = bufferWithNulls;

            var headers = MsmqUtilities.ExtractHeaders(message);

            Assert.AreEqual(expected, headers["NServiceBus.ExceptionInfo.Message"]);
        }

        [Test]
        public void Should_fetch_the_replyToAddress_from_responsequeue_for_backwards_compatibility()
        {
            var message = MsmqUtilities.Convert(new OutgoingMessage("message id", new Dictionary<string, string>(), new byte[0]), new DispatchOptions(new UnicastAddressTag("destination"), DispatchConsistency.Default));

            message.ResponseQueue = new MessageQueue(new MsmqAddress("local", Environment.MachineName).FullPath);
            var headers = MsmqUtilities.ExtractHeaders(message);

            Assert.AreEqual("local@" + Environment.MachineName, headers[Headers.ReplyToAddress]);
        }

        [Test]
        public void Should_use_the_TTBR_in_the_send_options_if_set()
        {
            var options = new DispatchOptions(new UnicastAddressTag("destination"), DispatchConsistency.Default, new List<DeliveryConstraint> { new DiscardIfNotReceivedBefore(TimeSpan.FromDays(1)) });

            var message = MsmqUtilities.Convert(new OutgoingMessage("message id", new Dictionary<string, string>(), new byte[0]), options);

            Assert.AreEqual(TimeSpan.FromDays(1), message.TimeToBeReceived);
        }


        [Test]
        public void Should_use_the_non_durable_setting()
        {
            var options = new DispatchOptions(new UnicastAddressTag("destination"), DispatchConsistency.Default, new List<DeliveryConstraint> { new NonDurableDelivery() });

            Assert.False(MsmqUtilities.Convert(new OutgoingMessage("message id", new Dictionary<string, string>(), new byte[0]), options).Recoverable);
            Assert.True(MsmqUtilities.Convert(new OutgoingMessage("message id", new Dictionary<string, string>(), new byte[0]), new DispatchOptions(new UnicastAddressTag("destination"), DispatchConsistency.Default, new List<DeliveryConstraint>())).Recoverable);
        }

    }
}