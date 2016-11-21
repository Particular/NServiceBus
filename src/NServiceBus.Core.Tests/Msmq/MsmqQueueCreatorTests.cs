namespace NServiceBus.Core.Tests.Msmq
{
    using System.Messaging;
    using System.Security.Principal;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class MsmqQueueCreatorTests
    {
        [Test]
        public void Should_create_all_queues()
        {
            var creator = new QueueCreator(true);
            var bindings = new QueueBindings();

            DeleteQueueIfPresent("MsmqQueueCreatorTests.receiver");
            DeleteQueueIfPresent("MsmqQueueCreatorTests.endpoint1");
            DeleteQueueIfPresent("MsmqQueueCreatorTests.endpoint2");

            bindings.BindReceiving("MsmqQueueCreatorTests.receiver");
            bindings.BindSending("MsmqQueueCreatorTests.target1");
            bindings.BindSending("MsmqQueueCreatorTests.target2");

            creator.CreateQueueIfNecessary(bindings, WindowsIdentity.GetCurrent().Name);

            Assert.True(QueueExists("MsmqQueueCreatorTests.receiver"));
            Assert.True(QueueExists("MsmqQueueCreatorTests.target1"));
            Assert.True(QueueExists("MsmqQueueCreatorTests.target1"));
        }

        [Test]
        public void Should_setup_permissions()
        {
            var testQueueName = "MsmqQueueCreatorTests.permissions";

            var creator = new QueueCreator(true);
            var bindings = new QueueBindings();

            DeleteQueueIfPresent(testQueueName);

            bindings.BindReceiving(testQueueName);

            // use the network service account since that one won't be in the local admin group
            creator.CreateQueueIfNecessary(bindings, NetworkServiceAccountName);

            var createdQueue = GetQueue(testQueueName);

            MessageQueueAccessRights? accountAccessRights;

            Assert.True(createdQueue.TryGetPermissions(NetworkServiceAccountName, out accountAccessRights));
            Assert.True(accountAccessRights.HasValue, "User should have access rights");
            Assert.AreEqual(MessageQueueAccessRights.WriteMessage | MessageQueueAccessRights.ReceiveMessage, accountAccessRights, "User should have write/read access");

            MessageQueueAccessRights? localAdminAccessRights;

            Assert.True(createdQueue.TryGetPermissions(LocalAdministratorsGroupName, out localAdminAccessRights));
            Assert.True(localAdminAccessRights.HasValue, "User should have access rights");
            Assert.AreEqual(MessageQueueAccessRights.FullControl, localAdminAccessRights, "LocalAdmins should have full control");
        }

        MessageQueue GetQueue(string queueName)
        {
            var path = MsmqAddress.Parse(queueName).PathWithoutPrefix;

            return new MessageQueue(path);
        }

        bool QueueExists(string queueName)
        {
            var path = MsmqAddress.Parse(queueName).PathWithoutPrefix;

            return MessageQueue.Exists(path);
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

        static string NetworkServiceAccountName = new SecurityIdentifier(WellKnownSidType.NetworkServiceSid, null).Translate(typeof(NTAccount)).ToString();
        static string LocalAdministratorsGroupName = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null).Translate(typeof(NTAccount)).ToString();
    }
}