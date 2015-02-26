namespace NServiceBus.Core.Tests.Msmq
{
    using NServiceBus.Support;
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

        [Test] public void GetSubScope_should_append_local_machine_name()
        {
            var msmqTransport = new MsmqTransport();
            var subScope = msmqTransport.GetSubScope("theQueue", "timeouts");
            subScope = subScope.Replace(RuntimeEnvironment.MachineName, "theMachine");
            Assert.AreEqual("theQueue.timeouts@theMachine", subScope);
        }
    }
}