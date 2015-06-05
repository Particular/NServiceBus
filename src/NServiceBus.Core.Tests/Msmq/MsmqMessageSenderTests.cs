﻿namespace NServiceBus.Core.Tests.Msmq
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;
    using NServiceBus.Transports.Msmq;
    using NServiceBus.Transports.Msmq.Config;
    using NUnit.Framework;

    [TestFixture]
    public class MsmqMessageSenderTests
    {
        class FakeContext : BehaviorContext
        {
            public FakeContext()
                : base(null)
            {
            }
        }


        [Test]
        public void Should_set_label_when_convention_configured()
        {
            var queueName = "labelTest";
            var path = string.Format(@"{0}\private$\{1}", Environment.MachineName, queueName);
            try
            {
                DeleteQueue(path);
                CreateQueue(path);
                var messageSender = new MsmqMessageSender(new FakeContext())
                {
                    Settings = new MsmqSettings(),
                    MessageLabelConvention = _ => "mylabel",
                };
                var bytes = new byte[]
                {
                    1
                };
                var headers = new Dictionary<string, string>();
                var outgoingMessage = new OutgoingMessage("1", headers, bytes);
                var sendOptions = new TransportSendOptions(queueName);
                messageSender.Send(outgoingMessage, sendOptions);
                var messageLabel = ReadMessageLabel(path);
                Assert.AreEqual("mylabel", messageLabel);

            }
            finally
            {
                DeleteQueue(path);
            }
        }
        [Test]
        public void Should_use_string_empty_label_when_no_convention_configured()
        {
            var queueName = "emptyLabelTest";
            var path = string.Format(@".\private$\{0}", queueName);
            try
            {
                DeleteQueue(path);
                CreateQueue(path);
                var messageSender = new MsmqMessageSender(new FakeContext())
                {
                    Settings = new MsmqSettings(),
                };
                var bytes = new byte[]
                {
                    1
                };
                var headers = new Dictionary<string, string>();
                var outgoingMessage = new OutgoingMessage("1", headers, bytes);
                var sendOptions = new TransportSendOptions(queueName);
                messageSender.Send(outgoingMessage, sendOptions);
                var messageLabel = ReadMessageLabel(path);
                Assert.IsEmpty(messageLabel);

            }
            finally
            {
                DeleteQueue(path);
            }
        }

        static void DeleteQueue(string path)
        {
            if (!MessageQueue.Exists(path))
            {
                return;
            }
            MessageQueue.Delete(path);
        }

        static string ReadMessageLabel(string path)
        {
            using (var queue = new MessageQueue(path))
            using (var message = queue.Receive(TimeSpan.FromSeconds(5)))
            {
                if (message != null)
                {
                    return message.Label;
                }
            }
            return null;
        }

        static void CreateQueue(string path)
        {
            if (MessageQueue.Exists(path))
            {
                return;
            }
            using (MessageQueue.Create(path, true))
            {
            }
        }
    }
}