namespace NServiceBus.Core.Tests.Msmq
{
    using NUnit.Framework;

    [TestFixture]
    public class MsmqTransportTests
    {
        [Test]
        public void GetSubScope_should_respect_machine_name()
        {
            var msmqTransport = new MsmqTransport();
            var subScope = msmqTransport.GetSubScope("theQueue@theMachine","timeouts");
            Assert.AreEqual("theQueue.timeouts@theMachine",subScope);
        }

        [Test]
        public void GetSubScope_should_not_append_machine_name()
        {
            var msmqTransport = new MsmqTransport();
            var subScope = msmqTransport.GetSubScope("theQueue", "timeouts");
            Assert.AreEqual("theQueue.timeouts", subScope);
        }
    }
}