namespace NServiceBus.Management.Retries.Tests
{
    using System;
    using System.Collections.Generic;
    using Core.Tests;
    using Faults.Forwarder;
    using NServiceBus.Faults;
    using SecondLevelRetries;
    using SecondLevelRetries.Helpers;
    using NUnit.Framework;
    using Transports;
    using Unicast;

    [TestFixture]
    public class SecondLevelRetriesTests
    {
        SecondLevelRetriesProcessor satellite ;
        FakeMessageSender messageSender ;
        FakeMessageDeferrer deferrer ;
        Address ERROR_QUEUE ;
        Address RETRIES_QUEUE ;
        Address ORIGINAL_QUEUE ;
        Address CLIENT_QUEUE ;

        TransportMessage message;

        [SetUp]
        public void SetUp()
        {
            messageSender = new FakeMessageSender();
            deferrer = new FakeMessageDeferrer();
            ERROR_QUEUE = new Address("error", "localhost");
            RETRIES_QUEUE = new Address("retries", "localhost");
            ORIGINAL_QUEUE = new Address("org", "hostname");
            CLIENT_QUEUE = Address.Parse("clientQ@myMachine");
            satellite = new SecondLevelRetriesProcessor(messageSender, deferrer, new FaultManager(new FuncBuilder(), null, null) {ErrorQueue = ERROR_QUEUE}, new ErrorSubscribersCoordinator(null))
            {
                InputAddress = RETRIES_QUEUE
            };

            message = new TransportMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>{{Headers.ReplyToAddress,CLIENT_QUEUE.ToString()}});
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
            TransportMessageHeaderHelper.SetHeader(message, FaultsHeaderKeys.FailedQ, "reply@address");


            for (var i = 0; i < satellite.NumberOfRetries + 1; i++)
            {
                satellite.Handle(message);
            }

            Assert.AreEqual(CLIENT_QUEUE, message.ReplyToAddress);
        }

        [Test]
        public void Message_should_be_sent_to_real_errorQ_if_defer_timeSpan_is_less_than_zero()
        {
            TransportMessageHeaderHelper.SetHeader(message, FaultsHeaderKeys.FailedQ, "reply@address");
            satellite.RetryPolicy = _ => TimeSpan.MinValue;

            satellite.Handle(message);

            Assert.AreEqual(ERROR_QUEUE, messageSender.MessageSentTo);
        }

        [Test]
        public void Message_should_be_sent_to_retryQ_if_defer_timeSpan_is_greater_than_zero()
        {
            TransportMessageHeaderHelper.SetHeader(message, FaultsHeaderKeys.FailedQ, "reply@address");
            satellite.RetryPolicy = _ => TimeSpan.FromSeconds(1);

            satellite.Handle(message);

            Assert.AreEqual(message, deferrer.DeferredMessage);
        }

        [Test]
        public void Message_should_only_be_retried_X_times_when_using_the_defaultPolicy()
        {
            TransportMessageHeaderHelper.SetHeader(message, FaultsHeaderKeys.FailedQ, "reply@address");

            for (var i = 0; i < satellite.NumberOfRetries + 1; i++)
            {
                satellite.Handle(message);
            }

            Assert.AreEqual(ERROR_QUEUE, messageSender.MessageSentTo);
        }

        [Test]
        public void Message_retries_header_should_be_removed_before_being_sent_to_real_errorQ()
        {
            TransportMessageHeaderHelper.SetHeader(message, FaultsHeaderKeys.FailedQ, "reply@address");

            satellite.Handle(message);

            TransportMessageHeaderHelper.SetHeader(message, SecondLevelRetriesHeaders.RetriesTimestamp, DateTimeExtensions.ToWireFormattedString(DateTime.Now.AddDays(-2)));
            
             satellite.Handle(message);

             Assert.False(message.Headers.ContainsKey(Headers.Retries));
        }

        [Test]
        public void A_message_should_only_be_able_to_retry_during_N_minutes()
        {
            TransportMessageHeaderHelper.SetHeader(message, FaultsHeaderKeys.FailedQ, "reply@address");
            TransportMessageHeaderHelper.SetHeader(message, SecondLevelRetriesHeaders.RetriesTimestamp, DateTimeExtensions.ToWireFormattedString(DateTime.Now.AddDays(-2)));
            satellite.Handle(message);

            Assert.AreEqual(ERROR_QUEUE, messageSender.MessageSentTo);
        }

        [Test]
        public void For_each_retry_the_NServiceBus_Retries_header_should_be_increased()
        {
            TransportMessageHeaderHelper.SetHeader(message, FaultsHeaderKeys.FailedQ, "reply@address");
            satellite.RetryPolicy = _ => TimeSpan.FromSeconds(1);            

            for (var i = 0; i < 10; i++)
            {
                satellite.Handle(message);
            }
            
            Assert.AreEqual(10, TransportMessageHeaderHelper.GetNumberOfRetries(message));            
        }

        [Test]
        public void Message_should_be_routed_to_the_failing_endpoint_when_the_time_is_up()
        {
            TransportMessageHeaderHelper.SetHeader(message, FaultsHeaderKeys.FailedQ, ORIGINAL_QUEUE.ToString());
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
