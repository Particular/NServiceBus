namespace NServiceBus.Core.Tests.Msmq
{
    using System;
    using System.Messaging;
    using System.Security.Principal;
    using NServiceBus.Utils;
    using NUnit.Framework;

    [TestFixture]
    public class MsmqExtensionsTests : MsmqTestsBase
    {
        static string LocalEveryoneGroupName = new SecurityIdentifier(WellKnownSidType.WorldSid, null).Translate(typeof(NTAccount)).ToString();
        static string LocalAnonymousLogonName = new SecurityIdentifier(WellKnownSidType.AnonymousSid, null).Translate(typeof(NTAccount)).ToString();

        string path;
        MessageQueue queue;

        [TestFixtureSetUp]
        public void Setup()
        {
            var queueName = "labelTest";
            path = string.Format(@"{0}\private$\{1}", Environment.MachineName, queueName);
            DeleteQueue(path);
            CreateQueue(path);

            queue = new MessageQueue(path);
        }

        [TestFixtureTearDown]
        public void Teardown()
        {
            queue.Dispose();
            DeleteQueue(path);
        }

        [Test]
        public void GetPermissions_returns_queue_access_rights()
        {
            queue.SetPermissions(LocalEveryoneGroupName, MessageQueueAccessRights.WriteMessage | MessageQueueAccessRights.ReceiveMessage, AccessControlEntryType.Allow);
            MessageQueueAccessRights? rights;
            if (!queue.TryGetPermissions(LocalEveryoneGroupName, out rights))
            {
                Assert.Fail("Unable to read permissions off a queue");
            }

            Assert.IsTrue(rights.HasValue);
            Assert.True(rights.Value.HasFlag(MessageQueueAccessRights.WriteMessage));
            Assert.True(rights.Value.HasFlag(MessageQueueAccessRights.ReceiveMessage));
        }
    }
}