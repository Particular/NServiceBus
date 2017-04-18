namespace NServiceBus.Core.Tests.Msmq
{
    using System;
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
            var creator = new MsmqQueueCreator(true);
            var bindings = new QueueBindings();

            bindings.BindReceiving("MsmqQueueCreatorTests.receiver");
            bindings.BindSending("MsmqQueueCreatorTests.target1");
            bindings.BindSending("MsmqQueueCreatorTests.target2");

            creator.CreateQueueIfNecessary(bindings, WindowsIdentity.GetCurrent().Name);

            Assert.True(QueueExists("MsmqQueueCreatorTests.receiver"));
            Assert.True(QueueExists("MsmqQueueCreatorTests.target1"));
            Assert.True(QueueExists("MsmqQueueCreatorTests.target2"));

            DeleteQueueIfPresent("MsmqQueueCreatorTests.receiver");
            DeleteQueueIfPresent("MsmqQueueCreatorTests.target1");
            DeleteQueueIfPresent("MsmqQueueCreatorTests.target2");
        }

        [Test]
        public void Should_not_create_remote_queues()
        {
            var creator = new MsmqQueueCreator(true);
            var bindings = new QueueBindings();
            var remoteQueueName = "MsmqQueueCreatorTests.remote";

            bindings.BindSending($"{remoteQueueName}@some-machine");

            creator.CreateQueueIfNecessary(bindings, WindowsIdentity.GetCurrent().Name);

            Assert.False(QueueExists(remoteQueueName));
        }


        [Test]
        public void Should_setup_permissions()
        {
            var testQueueName = "MsmqQueueCreatorTests.permissions";

            var creator = new MsmqQueueCreator(true);
            var bindings = new QueueBindings();

            bindings.BindReceiving(testQueueName);

            // use the network service account since that one won't be in the local admin group
            creator.CreateQueueIfNecessary(bindings, NetworkServiceAccountName);

            var createdQueue = GetQueue(testQueueName);

            MessageQueueAccessRights? accountAccessRights;
            AccessControlEntryType? accessControlEntryType;

            Assert.True(createdQueue.TryGetPermissions(NetworkServiceAccountName, out accountAccessRights, out accessControlEntryType));
            Assert.True(accountAccessRights.HasValue);
            Assert.True(accessControlEntryType == AccessControlEntryType.Allow, "User should have access");
            Assert.True(accountAccessRights?.HasFlag(MessageQueueAccessRights.WriteMessage), $"{NetworkServiceAccountName} should have write access");
            Assert.True(accountAccessRights?.HasFlag(MessageQueueAccessRights.ReceiveMessage), $"{NetworkServiceAccountName} should have receive messages access");

            MessageQueueAccessRights? localAdminAccessRights;
            AccessControlEntryType? accessControlEntryTypeForLocalAdmin;

            Assert.True(createdQueue.TryGetPermissions(LocalAdministratorsGroupName, out localAdminAccessRights, out accessControlEntryTypeForLocalAdmin));
            Assert.True(localAdminAccessRights.HasValue);
            Assert.True(localAdminAccessRights?.HasFlag(MessageQueueAccessRights.FullControl), $"{LocalAdministratorsGroupName} should have full control");
            Assert.IsTrue(accessControlEntryTypeForLocalAdmin == AccessControlEntryType.Allow, $"{LocalAdministratorsGroupName} should have access");

            DeleteQueueIfPresent(testQueueName);
        }

        [Test]
        public void Should_make_queues_transactional_if_requested()
        {
            var testQueueName = "MsmqQueueCreatorTests.txreceiver";

            var creator = new MsmqQueueCreator(true);
            var bindings = new QueueBindings();

            bindings.BindReceiving(testQueueName);

            creator.CreateQueueIfNecessary(bindings, WindowsIdentity.GetCurrent().Name);

            var queue = GetQueue(testQueueName);

            Assert.True(queue.Transactional);

            DeleteQueueIfPresent(testQueueName);
        }

        [Test]
        public void Should_make_queues_non_transactional_if_requested()
        {
            var testQueueName = "MsmqQueueCreatorTests.txreceiver";

            var creator = new MsmqQueueCreator(false);
            var bindings = new QueueBindings();

            bindings.BindReceiving(testQueueName);

            creator.CreateQueueIfNecessary(bindings, WindowsIdentity.GetCurrent().Name);

            var queue = GetQueue(testQueueName);

            Assert.False(queue.Transactional);

            DeleteQueueIfPresent(testQueueName);
        }

        [Test]
        public void Should_give_everyone_and_anonymous_access_rights_when_creating_queues()
        {
            var testQueueName = "MsmqQueueCreatorTests.MsmqDefaultPermissions";

            var path = MsmqAddress.Parse(testQueueName).PathWithoutPrefix;

            using (var queue = MessageQueue.Create(path))
            {
                MessageQueueAccessRights? everyoneAccessRights;
                AccessControlEntryType? accessControlEntryTypeForEveryone;

                Assert.True(queue.TryGetPermissions(LocalEveryoneGroupName, out everyoneAccessRights, out accessControlEntryTypeForEveryone));
                Assert.True(everyoneAccessRights.HasValue, $"{LocalEveryoneGroupName} should have access rights");
                Assert.True(everyoneAccessRights?.HasFlag(MessageQueueAccessRights.GenericWrite), $"{LocalEveryoneGroupName} should have GenericWrite access by default");
                Assert.True(accessControlEntryTypeForEveryone == AccessControlEntryType.Allow);

                MessageQueueAccessRights? anonymousAccessRights;
                AccessControlEntryType? accessControlEntryTypeForAnonymous;


                Assert.True(queue.TryGetPermissions(LocalAnonymousLogonName, out anonymousAccessRights, out accessControlEntryTypeForAnonymous));
                Assert.True(anonymousAccessRights.HasValue, $"{LocalAnonymousLogonName} should have access rights");
                Assert.True(anonymousAccessRights?.HasFlag(MessageQueueAccessRights.WriteMessage), $"{LocalAnonymousLogonName} should have write access by default");
                Assert.True(accessControlEntryTypeForAnonymous == AccessControlEntryType.Allow);
            }

            DeleteQueueIfPresent(testQueueName);
        }


        [Test]
        public void Should_not_add_everyone_and_anonymous_to_already_existing_queues()
        {
            var testQueueName = "MsmqQueueCreatorTests.NoChangesToExisting";

            var path = MsmqAddress.Parse(testQueueName).PathWithoutPrefix;

            using (var existingQueue = MessageQueue.Create(path))
            {
                existingQueue.SetPermissions(LocalEveryoneGroupName, MessageQueueAccessRights.GenericWrite, AccessControlEntryType.Revoke);
                existingQueue.SetPermissions(LocalAnonymousLogonName, MessageQueueAccessRights.WriteMessage, AccessControlEntryType.Revoke);
            }

            var creator = new MsmqQueueCreator(true);
            var bindings = new QueueBindings();

            bindings.BindReceiving(testQueueName);

            creator.CreateQueueIfNecessary(bindings, WindowsIdentity.GetCurrent().Name);


            var queue = GetQueue(testQueueName);

            MessageQueueAccessRights? nullBecauseRevoked;
            AccessControlEntryType? accessControlEntryType;

            Assert.False(queue.TryGetPermissions(LocalEveryoneGroupName, out nullBecauseRevoked, out accessControlEntryType));
            Assert.False(queue.TryGetPermissions(LocalAnonymousLogonName, out nullBecauseRevoked, out accessControlEntryType));
            Assert.IsNull(accessControlEntryType);

            DeleteQueueIfPresent(testQueueName);
        }

        [Test]
        public void Should_allow_queue_names_above_the_limit_for_set_permission()
        {
            var testQueueName = $"MsmqQueueCreatorTests.tolong.{Guid.NewGuid().ToString().Replace("-", "")}";

            var maxQueueNameForSetPermissionToWork = 102 - Environment.MachineName.Length;

            testQueueName = $"{testQueueName}{new string('a', maxQueueNameForSetPermissionToWork - testQueueName.Length + 1)}";

            var creator = new MsmqQueueCreator(true);
            var bindings = new QueueBindings();

            bindings.BindReceiving(testQueueName);

            Assert.DoesNotThrow(() => creator.CreateQueueIfNecessary(bindings, WindowsIdentity.GetCurrent().Name));

            DeleteQueueIfPresent(testQueueName);
        }

        [Test]
        public void Should_blow_up_for_invalid_accounts()
        {
            var testQueueName = "MsmqQueueCreatorTests.badidentity";

            var creator = new MsmqQueueCreator(true);
            var bindings = new QueueBindings();

            bindings.BindReceiving(testQueueName);

            var ex = Assert.Throws<InvalidOperationException>(() => creator.CreateQueueIfNecessary(bindings, "invalidaccount"));

            StringAssert.Contains("invalidaccount", ex.Message);

            DeleteQueueIfPresent(testQueueName);
        }

        [Test]
        public void Should_blow_up_if_name_is_null()
        {
            var creator = new MsmqQueueCreator(true);
            var bindings = new QueueBindings();

            bindings.BindReceiving(null);

            Assert.Throws<ArgumentNullException>(() => creator.CreateQueueIfNecessary(bindings, WindowsIdentity.GetCurrent().Name));
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


        static string LocalEveryoneGroupName = new SecurityIdentifier(WellKnownSidType.WorldSid, null).Translate(typeof(NTAccount)).ToString();
        static string LocalAnonymousLogonName = new SecurityIdentifier(WellKnownSidType.AnonymousSid, null).Translate(typeof(NTAccount)).ToString();
        static string NetworkServiceAccountName = new SecurityIdentifier(WellKnownSidType.NetworkServiceSid, null).Translate(typeof(NTAccount)).ToString();
        static string LocalAdministratorsGroupName = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null).Translate(typeof(NTAccount)).ToString();
    }
}