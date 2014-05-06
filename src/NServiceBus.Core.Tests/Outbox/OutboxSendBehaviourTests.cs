namespace NServiceBus.Core.Tests.Pipeline
{
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;
    using Outbox;
    using Unicast;

    [TestFixture]
    internal class When_a_transport_operation_is_performed_from_a_message_handler : SendBehaviourContext
    {
        [Test]
        public void Should_record_the_operation_for_later_storage_in_the_outbox()
        {
            var transportMessageBeingSent = new TransportMessage();

            var sendOptions = new SendOptions();

            var context = new SendPhysicalMessageContext(null, sendOptions, transportMessageBeingSent);

            context.Set(new OutboxMessage("1"));

            Assert.True(Invoke(context), "Pipeline should have been aborted");

            Assert.AreEqual(1, context.Get<OutboxMessage>().TransportOperations.Count);
        }
    }

    [TestFixture]
    internal class When_a_transport_operation_is_performed_from_a_non_message_handler : SendBehaviourContext
    {
        [Test]
        public void Should_allow_pipeline_to_proceed()
        {
            var transportMessageBeingSent = new TransportMessage();

            var sendOptions = new SendOptions();

            var context = new SendPhysicalMessageContext(null, sendOptions, transportMessageBeingSent);


            Assert.False(Invoke(context), "Pipeline should have not been aborted");
        }
    }

    [TestFixture]
    internal class When_dispatching_transport_operations : SendBehaviourContext
    {
        [Test]
        public void Should_allow_the_pipeline_to_continue()
        {
            var transportMessageBeingSent = new TransportMessage();

            var sendOptions = new SendOptions();
            var context = new SendPhysicalMessageContext(null, sendOptions, transportMessageBeingSent);
            var outboxMessage = new OutboxMessage("1");

            context.Set("Outbox_StartDispatching", true);

            context.Set(outboxMessage);

            Assert.False(Invoke(context), "Pipeline should not have been aborted");
        }
    }

    internal class SendBehaviourContext
    {
        [SetUp]
        public void SetUp()
        {
            fakeOutbox = new FakeOutboxStorage();

            behavior = new OutboxSendBehavior
            {
                OutboxStorage = fakeOutbox
            };
        }

        protected bool Invoke(SendPhysicalMessageContext context)
        {
            var aborted = true;

            behavior.Invoke(context, () => { aborted = false; });

            return aborted;
        }

        OutboxSendBehavior behavior;
        FakeOutboxStorage fakeOutbox;
    }
}