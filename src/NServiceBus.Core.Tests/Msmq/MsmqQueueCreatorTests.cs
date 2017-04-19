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
        string testQueueNameForSending = "NServiceBus.Core.Tests.MsmqQueueCreatorTests.Sending";
        string testQueueNameForReceiving = "NServiceBus.Core.Tests.MsmqQueueCreatorTests.Receiving";

        [SetUp]
        public void Setup()
        {
            DeleteQueueIfPresent(testQueueNameForSending);
            DeleteQueueIfPresent(testQueueNameForReceiving);
            DeleteQueueIfPresent(GetReallyLongQueueName());
        }

        [TearDown]
        public void TearDown()
        {
            DeleteQueueIfPresent(testQueueNameForSending);
            DeleteQueueIfPresent(testQueueNameForReceiving);
            DeleteQueueIfPresent(GetReallyLongQueueName());
        }

        [Test]
        public void Should_create_all_queues()
        {
            var creator = new MsmqQueueCreator(true);
            var bindings = new QueueBindings();

            bindings.BindReceiving(testQueueNameForReceiving);
            bindings.BindSending(testQueueNameForSending);

            creator.CreateQueueIfNecessary(bindings, WindowsIdentity.GetCurrent().Name);

            Assert.True(QueueExists(testQueueNameForSending));
            Assert.True(QueueExists(testQueueNameForReceiving));
        }

        [Test]
        public void Should_not_create_queue_when_a_remote_queue_is_provided()
        {
            var remoteQueueName = $"{testQueueNameForReceiving}@some-machine";
            var creator = new MsmqQueueCreator(true);
            var bindings = new QueueBindings();

            bindings.BindSending(remoteQueueName);

            creator.CreateQueueIfNecessary(bindings, WindowsIdentity.GetCurrent().Name);

            Assert.False(QueueExists(testQueueNameForReceiving));
        }


        [Test]
        public void Should_setup_permissions()
        {
            var creator = new MsmqQueueCreator(true);
            var bindings = new QueueBindings();

            bindings.BindReceiving(testQueueNameForReceiving);

            // use the network service account since that one won't be in the local admin group
            creator.CreateQueueIfNecessary(bindings, NetworkServiceAccountName);

            var createdQueue = GetQueue(testQueueNameForReceiving);

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
        }

        [Test]
        public void Should_make_queues_transactional_if_requested()
        {
            var creator = new MsmqQueueCreator(true);
            var bindings = new QueueBindings();

            bindings.BindReceiving(testQueueNameForReceiving);

            creator.CreateQueueIfNecessary(bindings, WindowsIdentity.GetCurrent().Name);

            var queue = GetQueue(testQueueNameForReceiving);

            Assert.True(queue.Transactional);
        }

        [Test]
        public void Should_make_queues_non_transactional_if_requested()
        {
            var creator = new MsmqQueueCreator(false);
            var bindings = new QueueBindings();

            bindings.BindReceiving(testQueueNameForReceiving);

            creator.CreateQueueIfNecessary(bindings, WindowsIdentity.GetCurrent().Name);

            var queue = GetQueue(testQueueNameForReceiving);

            Assert.False(queue.Transactional);
        }

        [Test]
        public void Should_give_everyone_and_anonymous_access_rights_when_creating_queues()
        {
            var path = MsmqAddress.Parse(testQueueNameForReceiving).PathWithoutPrefix;

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
        }


        [Test]
        public void Should_not_add_everyone_and_anonymous_to_already_existing_queues()
        {
            var path = MsmqAddress.Parse(testQueueNameForReceiving).PathWithoutPrefix;

            using (var existingQueue = MessageQueue.Create(path))
            {
                existingQueue.SetPermissions(LocalEveryoneGroupName, MessageQueueAccessRights.GenericWrite, AccessControlEntryType.Revoke);
                existingQueue.SetPermissions(LocalAnonymousLogonName, MessageQueueAccessRights.WriteMessage, AccessControlEntryType.Revoke);
            }

            var creator = new MsmqQueueCreator(true);
            var bindings = new QueueBindings();

            bindings.BindReceiving(testQueueNameForReceiving);

            creator.CreateQueueIfNecessary(bindings, WindowsIdentity.GetCurrent().Name);


            var queue = GetQueue(testQueueNameForReceiving);

            MessageQueueAccessRights? nullBecauseRevoked;
            AccessControlEntryType? accessControlEntryType;

            Assert.False(queue.TryGetPermissions(LocalEveryoneGroupName, out nullBecauseRevoked, out accessControlEntryType));
            Assert.False(queue.TryGetPermissions(LocalAnonymousLogonName, out nullBecauseRevoked, out accessControlEntryType));
            Assert.IsNull(accessControlEntryType);
        }

        [Test]
        public void Should_allow_queue_names_above_the_limit_for_set_permission()
        {
            var testQueueName = GetReallyLongQueueName();
            var creator = new MsmqQueueCreator(true);
            var bindings = new QueueBindings();

            bindings.BindReceiving(testQueueName);

            Assert.DoesNotThrow(() => creator.CreateQueueIfNecessary(bindings, WindowsIdentity.GetCurrent().Name));
        }

        [Test]
        public void Should_blow_up_for_invalid_accounts()
        {
            var creator = new MsmqQueueCreator(true);
            var bindings = new QueueBindings();

            bindings.BindReceiving(testQueueNameForReceiving);

            var ex = Assert.Throws<InvalidOperationException>(() => creator.CreateQueueIfNecessary(bindings, "invalidaccount"));

            StringAssert.Contains("invalidaccount", ex.Message);
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

            if (MessageQueue.Exists(path))
            {
                MessageQueue.Delete(path);
            }
        }

        string GetReallyLongQueueName()
        {
            var maxQueueNameForSetPermissionToWork = 102 - Environment.MachineName.Length;
            return testQueueNameForReceiving.PadRight(maxQueueNameForSetPermissionToWork - testQueueNameForReceiving.Length + 1, 'a');
        }


        static string LocalEveryoneGroupName = new SecurityIdentifier(WellKnownSidType.WorldSid, null).Translate(typeof(NTAccount)).ToString();
        static string LocalAnonymousLogonName = new SecurityIdentifier(WellKnownSidType.AnonymousSid, null).Translate(typeof(NTAccount)).ToString();
        static string NetworkServiceAccountName = new SecurityIdentifier(WellKnownSidType.NetworkServiceSid, null).Translate(typeof(NTAccount)).ToString();
        static string LocalAdministratorsGroupName = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null).Translate(typeof(NTAccount)).ToString();
    }
}