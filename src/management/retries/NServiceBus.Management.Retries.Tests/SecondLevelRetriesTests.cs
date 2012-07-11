using System;
using System.Collections.Generic;
using NServiceBus.Faults.Forwarder;
using NServiceBus.Management.Retries.Helpers;
using NServiceBus.Unicast.Queuing;
using NServiceBus.Unicast.Transport;
using NUnit.Framework;

namespace NServiceBus.Management.Retries.Tests
{
    [TestFixture]
    public class SecondLevelRetriesTests
    {
        readonly SecondLevelRetries _satellite = new SecondLevelRetries();
        readonly FakeMessageSender _messageSender = new FakeMessageSender();
        readonly Address ERROR_QUEUE = new Address("error","localhost");
        readonly Address RETRIES_QUEUE = new Address("retries", "localhost");
        readonly Address TIMEOUT_MANAGER_QUEUE = new Address("timeouts", "localhost");
        readonly Address ORIGINAL_QUEUE = new Address("org", "hostname");
        TransportMessage _message;

        [SetUp]
        public void SetUp()
        {
            _satellite.InputAddress = RETRIES_QUEUE;
            _satellite.FaultManager = new FaultManager {ErrorQueue = ERROR_QUEUE};
            _satellite.TimeoutManagerAddress = TIMEOUT_MANAGER_QUEUE;
            
            _satellite.MessageSender = _messageSender;

            SecondLevelRetries.RetryPolicy = DefaultRetryPolicy.Validate;
            SecondLevelRetries.TimeoutPolicy = DefaultRetryPolicy.HasTimedOut;

            _message = new TransportMessage {Headers = new Dictionary<string, string>()};
        }

        [Test]
        public void Message_should_have_ReplyToAddress_set_to_original_sender_when_sent_to_real_errorq()
        {
            var expected = new Address("clientQ", "myMachine");
            _message.ReplyToAddress = expected;
            SecondLevelRetries.RetryPolicy = _ => TimeSpan.MinValue;

            _satellite.Handle(_message);

            Assert.AreEqual(expected, _message.ReplyToAddress);
        }

        [Test]
        public void Message_should_have_ReplyToAddress_set_to_original_sender_when_sent_to_real_errorq_after_retries()
        {
            TransportMessageHelpers.SetHeader(_message, Faults.FaultsHeaderKeys.FailedQ, "reply@address");            

            var expected = new Address("clientQ", "myMachine");
            _message.ReplyToAddress = expected;

            for (var i = 0; i < DefaultRetryPolicy.NumberOfRetries + 1; i++)
            {
                _satellite.Handle(_message);
            }

            Assert.AreEqual(expected, _message.ReplyToAddress);
        }

        [Test]
        public void Message_should_be_sent_to_real_errorQ_if_defer_timespan_is_less_than_zero()
        {
            TransportMessageHelpers.SetHeader(_message, Faults.FaultsHeaderKeys.FailedQ, "reply@address");
            SecondLevelRetries.RetryPolicy = _ => { return TimeSpan.MinValue; };

            _satellite.Handle(_message);

            Assert.AreEqual(ERROR_QUEUE, _messageSender.MessageSentTo);
        }

        [Test]
        public void Message_should_be_sent_to_retryQ_if_defer_timespan_is_greater_than_zero()
        {
            TransportMessageHelpers.SetHeader(_message, Faults.FaultsHeaderKeys.FailedQ, "reply@address");                        
            SecondLevelRetries.RetryPolicy = _ => { return TimeSpan.FromSeconds(1); };

            _satellite.Handle(_message);

            Assert.AreEqual(TIMEOUT_MANAGER_QUEUE, _messageSender.MessageSentTo);
        }

        [Test]
        public void Message_should_only_be_retried_X_times_when_using_the_defaultPolicy()
        {
            TransportMessageHelpers.SetHeader(_message, Faults.FaultsHeaderKeys.FailedQ, "reply@address");            

            // default policy is to retry once every minute for 30 minutes
            for (int i = 0; i < 31; i++)
            {
                _satellite.Handle(_message);
            }

            Assert.AreEqual(ERROR_QUEUE, _messageSender.MessageSentTo);
        }

        [Test]
        public void A_message_should_only_be_able_to_retry_during_N_minutes()
        {
            TransportMessageHelpers.SetHeader(_message, Faults.FaultsHeaderKeys.FailedQ, "reply@address");            
            SecondLevelRetries.TimeoutPolicy = _ => { return true; };

            _satellite.Handle(_message);

            Assert.AreEqual(ERROR_QUEUE, _messageSender.MessageSentTo);
        }

        [Test]
        public void For_each_retry_the_NServiceBus_Retries_header_should_be_increased()
        {
            TransportMessageHelpers.SetHeader(_message, Faults.FaultsHeaderKeys.FailedQ, "reply@address");
            SecondLevelRetries.RetryPolicy = _ => { return TimeSpan.FromSeconds(1); };            

            for (int i = 0; i < 10; i++)
            {
                _satellite.Handle(_message);
            }
            
            Assert.AreEqual(10, TransportMessageHelpers.GetNumberOfRetries(_message));            
        }

        [Test]
        public void The_original_senders_address_should_be_used_as_ReplyToAddress()
        {
            TransportMessageHelpers.SetHeader(_message, Faults.FaultsHeaderKeys.FailedQ, ORIGINAL_QUEUE.ToString());
            SecondLevelRetries.RetryPolicy = _ => { return TimeSpan.FromSeconds(1); };

            _satellite.Handle(_message);

            Assert.AreEqual(ORIGINAL_QUEUE, _message.ReplyToAddress);            
        }
    }

    internal class FakeMessageSender : ISendMessages
    {
        public Address MessageSentTo { get; set; }

        public void Send(TransportMessage message, Address address)
        {
            MessageSentTo = address;
        }
    }
}
