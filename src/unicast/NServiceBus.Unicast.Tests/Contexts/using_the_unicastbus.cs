namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MasterNode;
    using MessageInterfaces.MessageMapper.Reflection;
    using MessageMutator;
    using NUnit.Framework;
    using ObjectBuilder;
    using Queuing;
    using Rhino.Mocks;
    using Rhino.Mocks.Generated;
    using Serialization;
    using Subscriptions;
    using Transport;

    public class using_a_configured_unicastbus
    {
        protected IBus bus;

        protected UnicastBus unicastBus;
        protected ISendMessages messageSender;
        protected FakeSubscriptionStorage subscriptionStorage;

        protected Address gatewayAddress;
        MessageHeaderManager headerManager = new MessageHeaderManager();
        MessageMapper messageMapper = new MessageMapper();

        [SetUp]
        public void SetUp()
        {
            string localAddress = "endpointA";
            Address masterNodeAddress = localAddress + "@MasterNode";

            try
            {
                Address.InitializeLocalAddress(localAddress);
            }
            catch // intentional
            {
            }

            ExtensionMethods.GetStaticOutgoingHeadersAction = () => MessageHeaderManager.staticHeaders;
            gatewayAddress = masterNodeAddress.SubScope("gateway");

            messageSender = MockRepository.GenerateStub<ISendMessages>();
            var masterNodeManager = MockRepository.GenerateStub<IManageTheMasterNode>();
            var builder = MockRepository.GenerateStub<IBuilder>();

            subscriptionStorage = new FakeSubscriptionStorage();

            builder.Stub(x => x.BuildAll<IMutateOutgoingMessages>()).Return(new IMutateOutgoingMessages[] { });

            builder.Stub(x => x.BuildAll<IMutateOutgoingTransportMessages>()).Return(new IMutateOutgoingTransportMessages[] { headerManager });

            masterNodeManager.Stub(x => x.GetMasterNode()).Return(masterNodeAddress);
            unicastBus = new UnicastBus
            {
                MessageSerializer = MockRepository.GenerateStub<IMessageSerializer>(),
                Builder = builder,
                MasterNodeManager = masterNodeManager,
                MessageSender = messageSender,
                Transport = MockRepository.GenerateStub<ITransport>(),
                SubscriptionStorage = subscriptionStorage,
                AutoSubscribe = true,
                MessageMapper = messageMapper
            };
            bus = unicastBus;
            ExtensionMethods.SetHeaderAction = headerManager.SetHeader;

        }

        protected void RegisterMessageHandlerType<T>()
        {
            unicastBus.MessageHandlerTypes = new[] { typeof(T) };
        }
        protected void RegisterOwnedMessageType<T>()
        {
            unicastBus.MessageOwners = new Dictionary<Type, Address> { { typeof(T),Address.Local} };
        }
        protected Address RegisterMessageType<T>()
        {
            var address = new Address(typeof(T).Name, "localhost");
            RegisterMessageType<T>(address);

            return address;
        }

        protected void RegisterMessageType<T>(Address address)
        {
            if (typeof(T).IsInterface)
                messageMapper.Initialize(new[] { typeof(T) });
            unicastBus.RegisterMessageType(typeof(T), address);

        }

        protected void StartBus()
        {
            ((IStartableBus)bus).Start();
        }

    }

    public class using_the_unicastbus : using_a_configured_unicastbus
    {
        [SetUp]
        public void SetUp()
        {
            StartBus();
        }
    }

    public class FakeSubscriptionStorage : ISubscriptionStorage
    {

        void ISubscriptionStorage.Subscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            messageTypes.ToList().ForEach(messageType =>
            {
                if (!storage.ContainsKey(messageType))
                    storage[messageType] = new List<Address>();

                if (!storage[messageType].Contains(address))
                    storage[messageType].Add(address);
            });
        }

        void ISubscriptionStorage.Unsubscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            messageTypes.ToList().ForEach(messageType =>
                                              {
                                                  if (storage.ContainsKey(messageType))
                                                      storage[messageType].Remove(address);
                                              });
        }


        IEnumerable<Address> ISubscriptionStorage.GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
        {
            var result = new List<Address>();
            messageTypes.ToList().ForEach(m =>
            {
                if (storage.ContainsKey(m))
                    result.AddRange(storage[m]);
            });

            return result;
        }
        public void FakeSubscribe<T>(Address address)
        {
            ((ISubscriptionStorage)this).Subscribe(address, new[] { new MessageType(typeof(T)) });
        }

        public void Init()
        {
        }

        readonly Dictionary<MessageType, List<Address>> storage = new Dictionary<MessageType, List<Address>>();
    }
}