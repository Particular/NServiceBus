namespace NServiceBus.Core.Tests.Msmq
{
    using System.IO;
    using System.Messaging;
    using System.Security.Principal;
    using System.Text;
    using NServiceBus.Logging;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class QueuePermissionsTests
    {
        StringBuilder logOutput;
        string testQueueName = "NServiceBus.Core.Tests.QueuePermissionsTests";

        [OneTimeSetUp]
        public void TestFixtureSetup()
        {
            var loggerFactory = LogManager.Use<TestingLoggerFactory>();
            loggerFactory.Level(LogLevel.Debug);
            logOutput = new StringBuilder();
            var stringWriter = new StringWriter(logOutput);
            loggerFactory.WriteTo(stringWriter);
        }


        [TearDown]
        public void TearDown()
        {
            var path = @".\private$\" + testQueueName;
            if (MessageQueue.Exists(path))
            {
                MessageQueue.Delete(path);
            }
            logOutput.Clear();
        }


        [TestCase(MessageQueueAccessRights.ChangeQueuePermissions)]
        [TestCase(MessageQueueAccessRights.DeleteQueue)]
        [TestCase(MessageQueueAccessRights.FullControl)]
        [TestCase(MessageQueueAccessRights.GenericRead)]
        [TestCase(MessageQueueAccessRights.GenericWrite)]
        [TestCase(MessageQueueAccessRights.ReceiveJournalMessage)]
        [TestCase(MessageQueueAccessRights.ReceiveMessage)]
        [TestCase(MessageQueueAccessRights.SetQueueProperties)]
        [TestCase(MessageQueueAccessRights.TakeQueueOwnership)]
        public void Should_not_warn_if_queue_has_public_access_set_to_deny(MessageQueueAccessRights accessRights)
        {
            // Set up a queue with the specified access for everyone/anonymous explicitly set to DENY. 
            var everyoneGroupName = new SecurityIdentifier(WellKnownSidType.WorldSid, null).Translate(typeof(NTAccount)).ToString();
            var anonymousGroupName = new SecurityIdentifier(WellKnownSidType.AnonymousSid, null).Translate(typeof(NTAccount)).ToString();

            using (var queue = MessageQueue.Create(@".\private$\" + testQueueName, false))
            {
                queue.SetPermissions(everyoneGroupName, accessRights, AccessControlEntryType.Deny);
                queue.SetPermissions(anonymousGroupName, accessRights, AccessControlEntryType.Deny);
            }

            QueuePermissions.CheckQueue(testQueueName);
            Assert.IsFalse(logOutput.ToString().Contains("Consider setting appropriate permissions"));

            // Resetting the queue permission to delete the queue to enable the cleanup of the unit test
            var path = @".\private$\" + testQueueName;
            using (var queueToModify = new MessageQueue(path))
            {
                queueToModify.SetPermissions(everyoneGroupName, MessageQueueAccessRights.DeleteQueue, AccessControlEntryType.Allow);
            }
        }

        [Test]
        public void Should_log_when_queue_is_remote()
        {
            var remoteQueue = $"{testQueueName}@remotemachine";

            QueuePermissions.CheckQueue(remoteQueue);

            Assert.That(logOutput.ToString(), Does.Contain($"{remoteQueue} is remote, the queue could not be verified."));
        }

        [Test]
        public void Should_warn_if_queue_doesnt_exist()
        {
            QueuePermissions.CheckQueue("NServiceBus.NonexistingQueueName");

            Assert.That(logOutput.ToString(), Does.Contain("does not exist"));
            Assert.That(logOutput.ToString(), Does.Contain("WARN"));
        }

        [Test]
        public void Should_log_if_queue_exists()
        {
            MessageQueue.Create(@".\private$\" + testQueueName, false);

            QueuePermissions.CheckQueue(testQueueName);

            Assert.That(logOutput.ToString(), Does.Contain("Verified that the queue"));
        }

        // MSMQ Access Rights are defined here: https://msdn.microsoft.com/en-us/library/system.messaging.messagequeueaccessrights(v=vs.110).aspx
        // FullControl, GenericWrite, GenericRead, ReceiveJournalMessage and ReceiveMessage are combination of rights. See above doco. 
        [TestCase(MessageQueueAccessRights.ChangeQueuePermissions, WellKnownSidType.WorldSid)]
        [TestCase(MessageQueueAccessRights.DeleteQueue, WellKnownSidType.WorldSid)]
        [TestCase(MessageQueueAccessRights.FullControl, WellKnownSidType.WorldSid)]
        [TestCase(MessageQueueAccessRights.GenericRead, WellKnownSidType.WorldSid)]
        [TestCase(MessageQueueAccessRights.GenericWrite, WellKnownSidType.WorldSid)]
        [TestCase(MessageQueueAccessRights.ReceiveJournalMessage, WellKnownSidType.WorldSid)]
        [TestCase(MessageQueueAccessRights.ReceiveMessage, WellKnownSidType.WorldSid)]
        [TestCase(MessageQueueAccessRights.SetQueueProperties, WellKnownSidType.WorldSid)]
        [TestCase(MessageQueueAccessRights.TakeQueueOwnership, WellKnownSidType.WorldSid)]
        [TestCase(MessageQueueAccessRights.ChangeQueuePermissions, WellKnownSidType.AnonymousSid)]
        [TestCase(MessageQueueAccessRights.DeleteQueue, WellKnownSidType.AnonymousSid)]
        [TestCase(MessageQueueAccessRights.FullControl, WellKnownSidType.AnonymousSid)]
        [TestCase(MessageQueueAccessRights.GenericRead, WellKnownSidType.AnonymousSid)]
        [TestCase(MessageQueueAccessRights.GenericWrite, WellKnownSidType.AnonymousSid)]
        [TestCase(MessageQueueAccessRights.ReceiveJournalMessage, WellKnownSidType.AnonymousSid)]
        [TestCase(MessageQueueAccessRights.ReceiveMessage, WellKnownSidType.AnonymousSid)]
        [TestCase(MessageQueueAccessRights.SetQueueProperties, WellKnownSidType.AnonymousSid)]
        [TestCase(MessageQueueAccessRights.TakeQueueOwnership, WellKnownSidType.AnonymousSid)]
        public void Should_warn_if_queue_has_public_access(MessageQueueAccessRights rights, WellKnownSidType sidType)
        {
            var groupName = new SecurityIdentifier(sidType, null).Translate(typeof(NTAccount)).ToString();
            using (var queue = MessageQueue.Create(@".\private$\" + testQueueName, false))
            {
                queue.SetPermissions(groupName, rights);
            }

            QueuePermissions.CheckQueue(testQueueName);
            Assert.That(logOutput.ToString(), Does.Contain("Consider setting appropriate permissions"));
        }
    }
}