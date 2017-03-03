namespace NServiceBus.Core.Tests.Msmq
{
    using System.Messaging;
    using NUnit.Framework;

    [TestFixture]
    public class MsmqQueuePermissionsTests
    {
        [Test]
        public void Should_Not_Check_Remote_Queues()
        {
            var result = QueuePermissions.CheckQueue("myqueue@remotemachine");
            Assert.IsNull(result);
        }

        [Test]
        public void Should_Check_Invalid_Local_Queues()
        {
            var result = QueuePermissions.CheckQueue("nonexistentqueue");
            Assert.IsFalse(result);
        }

        [Test]
        public void Should_Check_Valid_Local_Queues()
        {
            var msmq = MessageQueue.Create(@".\Private$\validqueuefortesting");
            var result = QueuePermissions.CheckQueue("validqueuefortesting");
            Assert.IsTrue(result);
            MessageQueue.Delete(@".\Private$\validqueuefortesting");
        }
    }
}
