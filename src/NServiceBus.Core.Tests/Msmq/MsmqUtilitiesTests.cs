namespace NServiceBus.Core.Tests.Msmq
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnostics.Windows;
    using BenchmarkDotNet.Running;
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
        public void Ensure_fast_method_deserializes_properly()
        {
            var expected = MsmqUtilities.DeserializeMessageHeaders(HeadersPerformanceTests.Msg);
            var parsed = MsmqUtilities.DeserializeMessageHeadersFast(HeadersPerformanceTests.Msg);

            CollectionAssert.AreEquivalent(expected, parsed);
        }

        [Test]
        [Explicit]
        public void BenchmarkParsing()
        {
            BenchmarkRunner.Run<HeadersPerformanceTests>();
        }
    }

    [Config(typeof(Config))]
    public class HeadersPerformanceTests
    {
        [Benchmark]
        public Dictionary<string, string> Current()
        {
            return MsmqUtilities.DeserializeMessageHeaders(Msg);
        }

        [Benchmark]
        public Dictionary<string, string> Fast()
        {
            return MsmqUtilities.DeserializeMessageHeadersFast(Msg);
        }

//<?xml version = "1.0" ?>
//< ArrayOfHeaderInfo xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
//  <HeaderInfo>
//    <Key>NServiceBus.ControlMessage</Key>
//    <Value>True</Value>
//  </HeaderInfo>
//  <HeaderInfo>
//    <Key>NServiceBus.MessageIntent</Key>
//    <Value>Subscribe</Value>
//  </HeaderInfo>
//  <HeaderInfo>
//    <Key>SubscriptionMessageType</Key>
//    <Value>NServiceBus.AcceptanceTests.Routing.When_subscribing_with_address_containing_host_name+MyEvent, NServiceBus.Msmq.AcceptanceTests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null</Value>
//  </HeaderInfo>
//  <HeaderInfo>
//    <Key>NServiceBus.ReplyToAddress</Key>
//    <Value>SubscribingWithAddressContainingHostName.Subscriber @DESKTOP-VFHGG46</Value>
//  </HeaderInfo>
//  <HeaderInfo>
//    <Key>NServiceBus.SubscriberAddress</Key>
//    <Value>SubscribingWithAddressContainingHostName.Subscriber @DESKTOP-VFHGG46</Value>
//  </HeaderInfo>
//  <HeaderInfo>
//    <Key>NServiceBus.SubscriberEndpoint</Key>
//    <Value>SubscribingWithAddressContainingHostName.Subscriber</Value>
//  </HeaderInfo>
//  <HeaderInfo>
//    <Key>NServiceBus.TimeSent</Key>
//    <Value>2016-06-30 13:02:46:369296 Z</Value>
//  </HeaderInfo>
//  <HeaderInfo>
//    <Key>NServiceBus.Version</Key>
//    <Value>6.0.0</Value>
//  </HeaderInfo>
//  <HeaderInfo>
//    <Key>CorrId</Key>
//    <Value />
//  </HeaderInfo>
//</ArrayOfHeaderInfo>

        internal static readonly Message Msg = MsmqUtilities.Convert(new OutgoingMessage("message id", new Dictionary<string, string>
        {
            {Headers.ControlMessageHeader, "True"},
            {Headers.MessageIntent, MessageIntentEnum.Subscribe.ToString()},
            {Headers.SubscriptionMessageType, "NServiceBus.AcceptanceTests.Routing.When_subscribing_with_address_containing_host_name+MyEvent, NServiceBus.Msmq.AcceptanceTests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"},
            {Headers.ReplyToAddress, "SubscribingWithAddressContainingHostName.Subscriber@DESKTOP"},
            {Headers.SubscriberTransportAddress, "SubscribingWithAddressContainingHostName.Subscriber@DESKTOP"},
            {Headers.SubscriberEndpoint, "SubscribingWithAddressContainingHostName.Subscriber"},
            {Headers.TimeSent, "2016-06-30 13:02:46:369296 Z"},
            {Headers.NServiceBusVersion, "6.0.0"},
            {Headers.CorrelationId, ""}
        }, new byte[0]), new List<DeliveryConstraint>());

        private class Config : ManualConfig
        {
            public Config()
            {
                Add(new MemoryDiagnoser());
            }
        }
    }
}