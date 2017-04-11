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
 
        [Test]
        public void Should_not_warn_if_queue_has_public_access_set_to_deny()
        {
            // Set up a queue with read/write access for everyone/anonymous explicitly set to DENY. 
            var everyoneGroupName = new SecurityIdentifier(WellKnownSidType.WorldSid, null).Translate(typeof(NTAccount)).ToString();
            var anonymousGroupName = new SecurityIdentifier(WellKnownSidType.AnonymousSid, null).Translate(typeof(NTAccount)).ToString();
            using (var queue = MessageQueue.Create(@".\private$\" + testQueueName, false))
            {
                queue.SetPermissions(everyoneGroupName, MessageQueueAccessRights.GenericRead, AccessControlEntryType.Deny);
                queue.SetPermissions(everyoneGroupName, MessageQueueAccessRights.GenericWrite, AccessControlEntryType.Deny);
                queue.SetPermissions(anonymousGroupName, MessageQueueAccessRights.GenericRead, AccessControlEntryType.Deny);
                queue.SetPermissions(anonymousGroupName, MessageQueueAccessRights.GenericWrite, AccessControlEntryType.Deny);
            }
            
            QueuePermissions.CheckQueue(testQueueName);
            Assert.IsFalse(logOutput.ToString().Contains("Consider setting appropriate permissions"));
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

        [TestCase(MessageQueueAccessRights.FullControl, WellKnownSidType.WorldSid)]
        [TestCase(MessageQueueAccessRights.GenericRead, WellKnownSidType.WorldSid)]
        [TestCase(MessageQueueAccessRights.GenericWrite, WellKnownSidType.WorldSid)]
        [TestCase(MessageQueueAccessRights.FullControl, WellKnownSidType.AnonymousSid)]
        [TestCase(MessageQueueAccessRights.GenericRead, WellKnownSidType.AnonymousSid)]
        [TestCase(MessageQueueAccessRights.GenericWrite, WellKnownSidType.AnonymousSid)]
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