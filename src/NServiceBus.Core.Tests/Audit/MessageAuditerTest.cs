namespace NServiceBus.Core.Tests.Audit
{
    using NServiceBus.Audit;
    using NServiceBus.Features;
    using NServiceBus.Transports;
    using NUnit.Framework;
    using Rhino.Mocks;

    [TestFixture]
    public class MessageAuditerTest
    {
        private ISendMessages messageSender;
        private AuditFilters auditFilters;
        
        private MessageAuditer testee;

        [SetUp]
        public void SetUp()
        {
            this.messageSender = MockRepository.GenerateStub<ISendMessages>();
            this.auditFilters = new AuditFilters();

            this.testee = new MessageAuditer
            {
                MessageSender = this.messageSender,
                AuditFilters = this.auditFilters
            };

            Feature.Enable<Audit>();
        }

        [Test]
        public void When_no_message_filter_declared_then_audit_message()
        {
            var transportMessage = new TransportMessage();

            this.testee.ForwardMessageToAuditQueue(transportMessage);

            messageSender.AssertWasCalled(s => s.Send(
                Arg<TransportMessage>.Matches(m => m.Id == transportMessage.Id),
                Arg<Address>.Is.Anything));
        }

        [Test]
        public void When_one_message_filter_returns_true_then_do_not_audit_message()
        {
            var transportMessage = new TransportMessage();
            auditFilters.ExcludeFromAudit(m => true);
            auditFilters.ExcludeFromAudit(m => false);

            this.testee.ForwardMessageToAuditQueue(transportMessage);

            messageSender.AssertWasNotCalled(s => s.Send(
                Arg<TransportMessage>.Is.Anything,
                Arg<Address>.Is.Anything));
        }

        [Test]
        public void When_all_message_filters_return_false_then_audit_message()
        {
            var transportMessage = new TransportMessage();
            auditFilters.ExcludeFromAudit(m => false);
            auditFilters.ExcludeFromAudit(m => false);

            this.testee.ForwardMessageToAuditQueue(transportMessage);

            messageSender.AssertWasCalled(s => s.Send(
                Arg<TransportMessage>.Matches(m => m.Id == transportMessage.Id),
                Arg<Address>.Is.Anything));
        }
    }
}