namespace NServiceBus.Core.Tests.Msmq
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Messaging;
    using DeliveryConstraints;
    using NServiceBus.Performance.TimeToBeReceived;
    using Transport;
    using NUnit.Framework;
    using Support;

    [TestFixture]
    public class MsmqUtilitiesTests
    {
        [Test]
        public void Should_convert_a_message_back_even_if_special_characters_are_contained_in_the_headers()
        {
            var expected = $"Can u see this '{(char) 0x19}' character.";

            var message = MsmqUtilities.Convert(new OutgoingMessage("message id", new Dictionary<string, string>
            {
                {"NServiceBus.ExceptionInfo.Message", expected}
            }, new byte[0]), new List<DeliveryConstraint>());
            var headers = MsmqUtilities.ExtractHeaders(message);

            Assert.AreEqual(expected, headers["NServiceBus.ExceptionInfo.Message"]);
        }

        [Test]
        public void Should_convert_message_headers_that_contain_nulls_at_the_end()
        {
            var expected = "Hello World";

            Console.Out.WriteLine(sizeof(char));
            var message = MsmqUtilities.Convert(new OutgoingMessage("message id", new Dictionary<string, string>
            {
                {"NServiceBus.ExceptionInfo.Message", expected}
            }, new byte[0]), new List<DeliveryConstraint>());
            var bufferWithNulls = new byte[message.Extension.Length + 10*sizeof(char)];

            Buffer.BlockCopy(message.Extension, 0, bufferWithNulls, 0, bufferWithNulls.Length - 10*sizeof(char));

            message.Extension = bufferWithNulls;

            var headers = MsmqUtilities.ExtractHeaders(message);

            Assert.AreEqual(expected, headers["NServiceBus.ExceptionInfo.Message"]);
        }

        [Test]
        public void Should_fetch_the_replyToAddress_from_responsequeue_for_backwards_compatibility()
        {
            var message = MsmqUtilities.Convert(
                new OutgoingMessage("message id", new Dictionary<string, string>(), new byte[0]),
                new List<DeliveryConstraint>());

            message.ResponseQueue = new MessageQueue(new MsmqAddress("local", RuntimeEnvironment.MachineName).FullPath);
            var headers = MsmqUtilities.ExtractHeaders(message);

            Assert.AreEqual("local@" + RuntimeEnvironment.MachineName, headers[Headers.ReplyToAddress]);
        }

        [Test]
        public void Should_use_the_TTBR_in_the_send_options_if_set()
        {
            var deliveryConstraints = new List<DeliveryConstraint>
            {
                new DiscardIfNotReceivedBefore(TimeSpan.FromDays(1))
            };

            var message = MsmqUtilities.Convert(new OutgoingMessage("message id", new Dictionary<string, string>(), new byte[0]), deliveryConstraints);

            Assert.AreEqual(TimeSpan.FromDays(1), message.TimeToBeReceived);
        }


        [Test]
        public void Should_use_the_non_durable_setting()
        {
            var nonDurableDeliveryConstraint = new List<DeliveryConstraint>
            {
                new NonDurableDelivery()
            };
            var durableDeliveryConstraint = new List<DeliveryConstraint>();

            Assert.False(MsmqUtilities.Convert(new OutgoingMessage("message id", new Dictionary<string, string>(), new byte[0]), nonDurableDeliveryConstraint).Recoverable);
            Assert.True(MsmqUtilities.Convert(new OutgoingMessage("message id", new Dictionary<string, string>(), new byte[0]), durableDeliveryConstraint).Recoverable);
        }

        [Test]
        public void Should_get_raw_array_segment_properly()
        {
            var bytes = new byte[]
            {
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9
            };

            const int testedLength = 8;
            var stream = new MemoryStream(bytes, 0, testedLength);
            var expected = new ArraySegment<byte>(bytes, 0, testedLength);

            CollectionAssert.AreEqual(expected, MsmqUtilities.GetAsArraySegment(stream));
        }
    }
}