namespace NServiceBus.Core.Tests.Msmq
{
    using System;
    using NServiceBus.Transports.Msmq;
    using NUnit.Framework;

    [TestFixture]
    public class TimeToBeRceivedOverrideCheckerTest
    {
        [Test]
        public void Should_not_throw_on_non_Msmq()
        {
            Assert.DoesNotThrow(() =>
            {
                TimeToBeReceivedOverrideChecker.Check(usingMsmq: false, isTransactional: false, outBoxRunning: false, auditTTBROverridden: false, forwardTTBROverridden: false);
            });
        }

        [Test]
        public void Should_not_throw_on_non_transactional()
        {
            Assert.DoesNotThrow(() =>
            {
                TimeToBeReceivedOverrideChecker.Check(usingMsmq: true, isTransactional: false, outBoxRunning: false, auditTTBROverridden: false, forwardTTBROverridden: false);
            });
        }

        [Test]
        public void Should_not_throw_on_enabled_outbox()
        {
            Assert.DoesNotThrow(() =>
            {
                TimeToBeReceivedOverrideChecker.Check(usingMsmq: true, isTransactional: true, outBoxRunning: true, auditTTBROverridden: false, forwardTTBROverridden: false);
            });
        }


        [Test]
        public void Should_throw_on_overridden_audit_TimeToBeReceived()
        {
            var exception = Assert.Throws<Exception>(() =>
            {
                TimeToBeReceivedOverrideChecker.Check(usingMsmq: true, isTransactional: true, outBoxRunning: false, auditTTBROverridden: true, forwardTTBROverridden: false);
            });

            Assert.AreEqual("Setting a custom OverrideTimeToBeReceived for audits is not supported on transactional MSMQ.", exception.Message);
        }

        [Test]
        public void Should_throw_on_overridden_TimeToBeReceivedOnForwardedMessages()
        {
            var exception = Assert.Throws<Exception>(() =>
            {
                TimeToBeReceivedOverrideChecker.Check(usingMsmq: true, isTransactional: true, outBoxRunning: false, auditTTBROverridden: false, forwardTTBROverridden: true);
            });

            Assert.AreEqual("Setting a custom TimeToBeReceivedOnForwardedMessages is not supported on transactional MSMQ.", exception.Message);
        }
    }
}