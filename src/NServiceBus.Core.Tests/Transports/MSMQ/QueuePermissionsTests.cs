namespace NServiceBus.Core.Tests.Transports.MSMQ
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
        string testQueueName = "moo";

        [TearDown]
        public void TearDown()
        {
            DeleteQueueIfPresent(testQueueName);
        }

        [Test]
        public void Should_log_message_when_queue_is_remote()
        {
            ConfigureLogger(LogLevel.Info);
            var machineName = "myremotemachine";
            var remoteQueue = $"{testQueueName}@{machineName}";

            QueuePermissions.CheckQueue(remoteQueue);

            Assert.That(logOutput.ToString(), Does.Contain("This endpoint cannot verify the existence of the remote queue"));
            Assert.That(logOutput.ToString(), Does.Contain(machineName));
        }

        [Test]
        public void Should_not_log_if_queue_doesnt_exist()
        {
            ConfigureLogger(LogLevel.Info);
            ClearLogger();
            DeleteQueueIfPresent(testQueueName);

            QueuePermissions.CheckQueue(testQueueName);

            Assert.IsEmpty(logOutput.ToString());
        }

        [Test]
        public void Should_log_message_if_queue_exists()
        {
            ConfigureLogger(LogLevel.Debug);
            var queueAddress = MsmqAddress.Parse(testQueueName);
            CreateQueue(queueAddress);
            
            QueuePermissions.CheckQueue(testQueueName);    

            Assert.That(logOutput.ToString(), Does.Contain($"Verified that the queue: [{queueAddress.PathWithoutPrefix}] exists"));
        }

        void CreateQueue(MsmqAddress address)
        {
            DeleteQueueIfPresent(address.Queue);
            var identity = WindowsIdentity.GetCurrent().Name;
            var localAdministratorsGroupName = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null).Translate(typeof(NTAccount)).ToString();

            using (var queue = MessageQueue.Create(address.PathWithoutPrefix, false))
            {
                queue.SetPermissions(identity, MessageQueueAccessRights.WriteMessage);
                queue.SetPermissions(identity, MessageQueueAccessRights.ReceiveMessage);
                queue.SetPermissions(identity, MessageQueueAccessRights.PeekMessage);

                queue.SetPermissions(localAdministratorsGroupName, MessageQueueAccessRights.FullControl);
            }
        }

        void DeleteQueueIfPresent(string queueName)
        {
            var path = MsmqAddress.Parse(queueName).PathWithoutPrefix;

            if (!MessageQueue.Exists(path))
            {
                return;
            }

            MessageQueue.Delete(path);
        }

        void ConfigureLogger(LogLevel logLevel)
        {
            var loggerFactory = LogManager.Use<TestingLoggerFactory>();
            loggerFactory.Level(logLevel);
            logOutput = new StringBuilder();
            var stringWriter = new StringWriter(logOutput);
            loggerFactory.WriteTo(stringWriter);
        }

        void ClearLogger()
        {
            QueuePermissions.CheckQueue("dummy");
            logOutput.Clear();
        }
    }
}