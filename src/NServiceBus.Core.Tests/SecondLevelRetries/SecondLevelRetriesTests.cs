namespace NServiceBus.Management.Retries.Tests
{
    using System;
    using System.Collections.Generic;
    using Faults.Forwarder;
    using SecondLevelRetries;
    using SecondLevelRetries.Helpers;
    using NUnit.Framework;
    using Transports;
    using Unicast;

    [TestFixture]
    public class SecondLevelRetriesTests
    {
        SecondLevelRetriesProcessor satellite = new SecondLevelRetriesProcessor();
        FakeMessageSender messageSender = new FakeMessageSender();
        FakeMessageDeferrer deferrer = new FakeMessageDeferrer();
        Address ERROR_QUEUE = new Address("error","localhost");
        Address RETRIES_QUEUE = new Address("retries", "localhost");
        Address ORIGINAL_QUEUE = new Address("org", "hostname");
        Address CLIENT_QUEUE = Address.Parse("clientQ@myMachine");

        TransportMessage message;

        [SetUp]
        public void SetUp()
        {
            var configure = Configure.With();
            satellite.InputAddress = RETRIES_QUEUE;
            satellite.FaultManager = new FaultManager(configure) { ErrorQueue = ERROR_QUEUE };
            
            satellite.MessageSender = messageSender;
            satellite.MessageDeferrer = deferrer;

            satellite.RetryPolicy = DefaultRetryPolicy.RetryPolicy;

            message = new TransportMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), CLIENT_QUEUE);
        }

        [Test]
        public void Message_should_have_ReplyToAddress_set_to_original_sender_when_sent_to_real_error_queue()
        {
            satellite.RetryPolicy = _ => TimeSpan.MinValue;

            satellite.Handle(message);

            Assert.AreEqual(CLIENT_QUEUE, message.ReplyToAddress);
        }

        [Test]
        public void Message_should_have_ReplyToAddress_set_to_original_sender_when_sent_to_real_error_queue_after_retries()
        {
            TransportMessageHelpers.SetHeader(message, Faults.FaultsHeaderKeys.FailedQ, "reply@address");            

          
            for (var i = 0; i < DefaultRetryPolicy.NumberOfRetries + 1; i++)
            {
                satellite.Handle(message);
            }

            Assert.AreEqual(CLIENT_QUEUE, message.ReplyToAddress);
        }

        [Test]
        public void Message_should_be_sent_to_real_errorQ_if_defer_timeSpan_is_less_than_zero()
        {
            TransportMessageHelpers.SetHeader(message, Faults.FaultsHeaderKeys.FailedQ, "reply@address");
            satellite.RetryPolicy = _ => TimeSpan.MinValue;

            satellite.Handle(message);

            Assert.AreEqual(ERROR_QUEUE, messageSender.MessageSentTo);
        }

        [Test]
        public void Message_should_be_sent_to_retryQ_if_defer_timeSpan_is_greater_than_zero()
        {
            TransportMessageHelpers.SetHeader(message, Faults.FaultsHeaderKeys.FailedQ, "reply@address");
            satellite.RetryPolicy = _ => TimeSpan.FromSeconds(1);

            satellite.Handle(message);

            Assert.AreEqual(message, deferrer.DeferredMessage);
        }

        [Test]
        public void Message_should_only_be_retried_X_times_when_using_the_defaultPolicy()
        {
            TransportMessageHelpers.SetHeader(message, Faults.FaultsHeaderKeys.FailedQ, "reply@address");

            for (var i = 0; i < DefaultRetryPolicy.NumberOfRetries + 1; i++)
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

            TransportMessageHelpers.SetHeader(message, SecondLevelRetriesHeaders.RetriesTimestamp, DateTimeExtensions.ToWireFormattedString(DateTime.Now.AddDays(-2)));
            
             satellite.Handle(message);

             Assert.False(message.Headers.ContainsKey(Headers.Retries));
        }

        [Test]
        public void A_message_should_only_be_able_to_retry_during_N_minutes()
        {
            TransportMessageHelpers.SetHeader(message, Faults.FaultsHeaderKeys.FailedQ, "reply@address");
            TransportMessageHelpers.SetHeader(message, SecondLevelRetriesHeaders.RetriesTimestamp, DateTimeExtensions.ToWireFormattedString(DateTime.Now.AddDays(-2)));
            satellite.Handle(message);

            Assert.AreEqual(ERROR_QUEUE, messageSender.MessageSentTo);
        }

        [Test]
        public void For_each_retry_the_NServiceBus_Retries_header_should_be_increased()
        {
            TransportMessageHelpers.SetHeader(message, Faults.FaultsHeaderKeys.FailedQ, "reply@address");
            satellite.RetryPolicy = _ => TimeSpan.FromSeconds(1);            

            for (var i = 0; i < 10; i++)
            {
                satellite.Handle(message);
            }
            
            Assert.AreEqual(10, TransportMessageHelpers.GetNumberOfRetries(message));            
        }

        [Test]
        public void Message_should_be_routed_to_the_failing_endpoint_when_the_time_is_up()
        {
            TransportMessageHelpers.SetHeader(message, Faults.FaultsHeaderKeys.FailedQ, ORIGINAL_QUEUE.ToString());
            satellite.RetryPolicy = _ => TimeSpan.FromSeconds(1);

            satellite.Handle(message);

            Assert.AreEqual(ORIGINAL_QUEUE, deferrer.MessageRoutedTo);            
        }
    }

    class FakeMessageDeferrer : IDeferMessages
    {
        public Address MessageRoutedTo { get; set; }

        public TransportMessage DeferredMessage { get; set; }

        public void Defer(TransportMessage message, SendOptions sendOptions)
        {
            MessageRoutedTo = sendOptions.Destination;
            DeferredMessage = message;
        }

        public void ClearDeferredMessages(string headerKey, string headerValue)
        {
            
        }
    }

    class FakeMessageSender : ISendMessages
    {
        public Address MessageSentTo { get; set; }

        public void Send(TransportMessage message, SendOptions sendOptions)
        {
            MessageSentTo = sendOptions.Destination;
        }
    }
}
