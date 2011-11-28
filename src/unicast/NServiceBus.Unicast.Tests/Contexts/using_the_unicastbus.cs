namespace NServiceBus.Unicast.Tests.Contexts
{
    using System;
    using System.Collections.Generic;
    using Helpers;
    using NServiceBus.Faults;
    using NServiceBus.MasterNode;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
    using NServiceBus.MessageMutator;
    using NUnit.Framework;
    using NServiceBus.Unicast.Queuing;
    using Rhino.Mocks;
    using NServiceBus.Serializers.XML;
    using NServiceBus.Unicast.Transport;
    using NServiceBus.UnitOfWork;

    public class using_a_configured_unicastbus
    {
        protected IBus bus;

        protected UnicastBus unicastBus;
        protected ISendMessages messageSender;
        protected FakeSubscriptionStorage subscriptionStorage;

        protected Address gatewayAddress;
        MessageHeaderManager headerManager = new MessageHeaderManager();
        MessageMapper MessageMapper = new MessageMapper();

        protected FakeTransport Transport = new FakeTransport();
        protected IList<IMessageModule> MessageModules = new List<IMessageModule>();
        protected XmlMessageSerializer MessageSerializer;
        protected FuncBuilder FuncBuilder = new FuncBuilder();

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

            MessageSerializer = new XmlMessageSerializer(MessageMapper);
            ExtensionMethods.GetStaticOutgoingHeadersAction = () => MessageHeaderManager.staticHeaders;
            gatewayAddress = masterNodeAddress.SubScope("gateway");

            messageSender = MockRepository.GenerateStub<ISendMessages>();
            var masterNodeManager = MockRepository.GenerateStub<IManageTheMasterNode>();

            subscriptionStorage = new FakeSubscriptionStorage();
            FuncBuilder.Register<IMutateOutgoingTransportMessages>(()=>headerManager);

            masterNodeManager.Stub(x => x.GetMasterNode()).Return(masterNodeAddress);
            unicastBus = new UnicastBus
            {
                MessageSerializer = MessageSerializer,
                Builder = FuncBuilder,
                MasterNodeManager = masterNodeManager,
                MessageSender = messageSender,
                Transport = Transport,
                SubscriptionStorage = subscriptionStorage,
                AutoSubscribe = true,
                MessageMapper = MessageMapper,
                FailureManager = MockRepository.GenerateStub<IManageMessageFailures>()
            };
            bus = unicastBus;
            ExtensionMethods.SetHeaderAction = headerManager.SetHeader;

        }

        protected void RegisterUow(IManageUnitsOfWork uow)
        {
            FuncBuilder.Register<IManageUnitsOfWork>(() => uow);
        }


        protected void RegisterMessageHandlerType<T>()
        {
            unicastBus.MessageHandlerTypes = new[] { typeof(T) };
        }
        protected void RegisterOwnedMessageType<T>()
        {
            unicastBus.MessageOwners = new Dictionary<Type, Address> { { typeof(T), Address.Local } };
        }
        protected Address RegisterMessageType<T>()
        {
            var address = new Address(typeof(T).Name, "localhost");
            RegisterMessageType<T>(address);

            return address;
        }

        protected void RegisterMessageType<T>(Address address)
        {
            MessageMapper.Initialize(new[] { typeof(T) });
            MessageSerializer.Initialize(new[] { typeof(T) });
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

        protected Exception ResultingException;
        protected void ReceiveMessage(TransportMessage transportMessage)
        {
            try
            {
                Transport.FakeMessageBeeingProcessed(transportMessage);
            }
            catch (Exception ex)
            {
                ResultingException = ex; 
            }
        }

        protected void SimulateMessageBeeingAbortedDueToRetryCountExceeded(TransportMessage transportMessage)
        {
            try
            {
                Transport.FakeMessageBeeingPassedToTheFaultManager(transportMessage);
            }
            catch (Exception ex)
            {
                ResultingException = ex;
            }
        }
    }
}