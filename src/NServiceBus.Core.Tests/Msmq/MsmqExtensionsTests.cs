namespace NServiceBus.Core.Tests.Msmq
{
    using System.Messaging;
    using System.Security.Principal;
    using NUnit.Framework;
    using Support;

    [TestFixture]
    public class MsmqExtensionsTests
    {
        static readonly string LocalEveryoneGroupName = new SecurityIdentifier(WellKnownSidType.WorldSid, null).Translate(typeof(NTAccount)).ToString();

        string path;
        MessageQueue queue;

        [OneTimeSetUp]
        public void Setup()
        {
            var queueName = "permissionsTest";
            path = $@"{RuntimeEnvironment.MachineName}\private$\{queueName}";
            MsmqHelpers.DeleteQueue(path);
            MsmqHelpers.CreateQueue(path);

            queue = new MessageQueue(path);
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            queue.Dispose();
            MsmqHelpers.DeleteQueue(path);
        }

        [TestCase(AccessControlEntryType.Allow)]
        [TestCase(AccessControlEntryType.Deny)]
        public void GetPermissions_returns_queue_access_rights(AccessControlEntryType providedAccessType)
        {
            queue.SetPermissions(LocalEveryoneGroupName, MessageQueueAccessRights.WriteMessage | MessageQueueAccessRights.ReceiveMessage, providedAccessType);
            MessageQueueAccessRights? rights;
            AccessControlEntryType? accessType;
            if (!queue.TryGetPermissions(LocalEveryoneGroupName, out rights, out accessType))
            {
                Assert.Fail($"Unable to read permissions for queue: {queue.QueueName}");
            }

            Assert.IsTrue(rights.HasValue);
            Assert.True(rights.Value.HasFlag(MessageQueueAccessRights.WriteMessage));
            Assert.True(rights.Value.HasFlag(MessageQueueAccessRights.ReceiveMessage));
            Assert.That(accessType == providedAccessType);
        }
    }
}