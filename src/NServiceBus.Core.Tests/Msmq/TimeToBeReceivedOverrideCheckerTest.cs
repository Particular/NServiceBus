namespace NServiceBus.Core.Tests.Msmq
{
    using NUnit.Framework;

    [TestFixture]
    public class TimeToBeReceivedOverrideCheckerTest
    {
        [Test]
        public void Should_succeed_on_non_Msmq()
        {
            var result = TimeToBeReceivedOverrideChecker.Check(usingMsmq: false, isTransactional: false, outBoxRunning: false, auditTTBROverridden: false, forwardTTBROverridden: false);
            Assert.IsTrue(result.Succeeded);
        }

        [Test]
        public void Should_succeed_on_non_transactional()
        {
            var result = TimeToBeReceivedOverrideChecker.Check(usingMsmq: true, isTransactional: false, outBoxRunning: false, auditTTBROverridden: false, forwardTTBROverridden: false);
            Assert.IsTrue(result.Succeeded);
        }

        [Test]
        public void Should_succeed_on_enabled_outbox()
        {
            var result = TimeToBeReceivedOverrideChecker.Check(usingMsmq: true, isTransactional: true, outBoxRunning: true, auditTTBROverridden: false, forwardTTBROverridden: false);
            Assert.IsTrue(result.Succeeded);
        }


        [Test]
        public void Should_fail_on_overridden_audit_TimeToBeReceived()
        {
            var result = TimeToBeReceivedOverrideChecker.Check(usingMsmq: true, isTransactional: true, outBoxRunning: false, auditTTBROverridden: true, forwardTTBROverridden: false);
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("Setting a custom OverrideTimeToBeReceived for audits is not supported on transactional MSMQ.", result.ErrorMessage);
        }

        [Test]
        public void Should_fail_on_overridden_TimeToBeReceivedOnForwardedMessages()
        {
            var result = TimeToBeReceivedOverrideChecker.Check(usingMsmq: true, isTransactional: true, outBoxRunning: false, auditTTBROverridden: false, forwardTTBROverridden: true);
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("Setting a custom TimeToBeReceivedOnForwardedMessages is not supported on transactional MSMQ.", result.ErrorMessage);
        }
    }
}