using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NServiceBus.Management.Retries.Tests
{
    using Faults.Forwarder;
    using Helpers;
    using Unicast.Queuing;

    [TestFixture]
    public class SecondLevelRetriesTests
    {
        readonly SecondLevelRetries satellite = new SecondLevelRetries();
        readonly FakeMessageSender messageSender = new FakeMessageSender();
        readonly FakeMessageDeferrer deferrer = new FakeMessageDeferrer();
        readonly Address ERROR_QUEUE = new Address("error","localhost");
        readonly Address RETRIES_QUEUE = new Address("retries", "localhost");
        readonly Address TIMEOUT_MANAGER_QUEUE = new Address("timeouts", "localhost");
        readonly Address ORIGINAL_QUEUE = new Address("org", "hostname");
        TransportMessage message;

        [SetUp]
        public void SetUp()
        {
            satellite.InputAddress = RETRIES_QUEUE;
            satellite.FaultManager = new FaultManager {ErrorQueue = ERROR_QUEUE};
            
            satellite.MessageSender = messageSender;
            satellite.MessageDeferrer = deferrer;

            SecondLevelRetries.RetryPolicy = DefaultRetryPolicy.Validate;
            SecondLevelRetries.TimeoutPolicy = DefaultRetryPolicy.HasTimedOut;

            message = new TransportMessage {Headers = new Dictionary<string, string>()};
        }

        [Test]
        public void Message_should_have_ReplyToAddress_set_to_original_sender_when_sent_to_real_errorq()
        {
            var expected = new Address("clientQ", "myMachine");
            message.ReplyToAddress = expected;
            SecondLevelRetries.RetryPolicy = _ => TimeSpan.MinValue;

            satellite.Handle(message);

            Assert.AreEqual(expected, message.ReplyToAddress);
        }

        [Test]
        public void Message_should_have_ReplyToAddress_set_to_original_sender_when_sent_to_real_errorq_after_retries()
        {
            TransportMessageHelpers.SetHeader(message, Faults.FaultsHeaderKeys.FailedQ, "reply@address");            

            var expected = new Address("clientQ", "myMachine");
            message.ReplyToAddress = expected;

            for (var i = 0; i < DefaultRetryPolicy.NumberOfRetries + 1; i++)
            {
                satellite.Handle(message);
            }

            Assert.AreEqual(expected, message.ReplyToAddress);
        }

        [Test]
        public void Message_should_be_sent_to_real_errorQ_if_defer_timespan_is_less_than_zero()
        {
            TransportMessageHelpers.SetHeader(message, Faults.FaultsHeaderKeys.FailedQ, "reply@address");
            SecondLevelRetries.RetryPolicy = _ => { return TimeSpan.MinValue; };

            satellite.Handle(message);

            Assert.AreEqual(ERROR_QUEUE, messageSender.MessageSentTo);
        }

        [Test]
        public void Message_should_be_sent_to_retryQ_if_defer_timespan_is_greater_than_zero()
        {
            TransportMessageHelpers.SetHeader(message, Faults.FaultsHeaderKeys.FailedQ, "reply@address");                        
            SecondLevelRetries.RetryPolicy = _ => { return TimeSpan.FromSeconds(1); };

            satellite.Handle(message);

            Assert.AreEqual(message, deferrer.DeferredMessage);
        }

        [Test]
        public void Message_should_only_be_retried_X_times_when_using_the_defaultPolicy()
        {
            TransportMessageHelpers.SetHeader(message, Faults.FaultsHeaderKeys.FailedQ, "reply@address");

            for (int i = 0; i < DefaultRetryPolicy.NumberOfRetries + 1; i++)
            {
                satellite.Handle(message);
            }

            Assert.AreEqual(ERROR_QUEUE, messageSender.MessageSentTo);
        }

        [Test]
        public void Message_retries_header_should_be_removed_before_being_sent_to_real_errorQ()
        {
            TransportMessageHelpers.SetHeader(message, Faults.FaultsHeaderKeys.FailedQ, "reply@address");

            satellite.Handle(message);

            SecondLevelRetries.TimeoutPolicy = _ => true;
            
             satellite.Handle(message);

             Assert.False(message.Headers.ContainsKey(Headers.Retries));
        }

        [Test]
        public void A_message_should_only_be_able_to_retry_during_N_minutes()
        {
            TransportMessageHelpers.SetHeader(message, Faults.FaultsHeaderKeys.FailedQ, "reply@address");            
            SecondLevelRetries.TimeoutPolicy = _ => { return true; };

            satellite.Handle(message);

            Assert.AreEqual(ERROR_QUEUE, messageSender.MessageSentTo);
        }

        [Test]
        public void For_each_retry_the_NServiceBus_Retries_header_should_be_increased()
        {
            TransportMessageHelpers.SetHeader(message, Faults.FaultsHeaderKeys.FailedQ, "reply@address");
            SecondLevelRetries.RetryPolicy = _ => { return TimeSpan.FromSeconds(1); };            

            for (int i = 0; i < 10; i++)
            {
                satellite.Handle(message);
            }
            
            Assert.AreEqual(10, TransportMessageHelpers.GetNumberOfRetries(message));            
        }

        [Test]
        public void Message_should_be_routed_to_the_failing_endpoint_when_the_time_is_up()
        {
            TransportMessageHelpers.SetHeader(message, Faults.FaultsHeaderKeys.FailedQ, ORIGINAL_QUEUE.ToString());
            SecondLevelRetries.RetryPolicy = _ => TimeSpan.FromSeconds(1);

            satellite.Handle(message);

            Assert.AreEqual(ORIGINAL_QUEUE, deferrer.MessageRoutedTo);            
        }
    }

    internal class FakeMessageDeferrer : IDeferMessages
    {
        public Address MessageRoutedTo { get; set; }

        public TransportMessage DeferredMessage { get; set; }

        public void Defer(TransportMessage message, DateTime processAt, Address address)
        {
            MessageRoutedTo = address;
            DeferredMessage = message;
        }

        public void ClearDeferredMessages(string headerKey, string headerValue)
        {
            
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
