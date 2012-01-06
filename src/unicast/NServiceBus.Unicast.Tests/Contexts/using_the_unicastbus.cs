namespace NServiceBus.Unicast.Tests.Contexts
{
    using System;
    using System.Collections.Generic;
    using Helpers;
    using Faults;
    using MessageInterfaces.MessageMapper.Reflection;
    using MessageMutator;
    using NUnit.Framework;
    using Queuing;
    using Rhino.Mocks;
    using Serializers.XML;
    using Timing;
    using Transport;
    using UnitOfWork;

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
        protected Address MasterNodeAddress;

        [SetUp]
        public void SetUp()
        {
            Configure.GetEndpointNameAction = () => "TestEndpoint";
            const string localAddress = "endpointA";
            MasterNodeAddress = new Address(localAddress,"MasterNode"); 

            try
            {
                Address.InitializeLocalAddress(localAddress);
            }
            catch // intentional
            {
            }

            MessageSerializer = new XmlMessageSerializer(MessageMapper);
            ExtensionMethods.GetStaticOutgoingHeadersAction = () => MessageHeaderManager.staticHeaders;
            gatewayAddress = MasterNodeAddress.SubScope("gateway");

            messageSender = MockRepository.GenerateStub<ISendMessages>();

            subscriptionStorage = new FakeSubscriptionStorage();
            FuncBuilder.Register<IMutateOutgoingTransportMessages>(()=>headerManager);
            FuncBuilder.Register<IMutateOutgoingTransportMessages>(() => new SentTimeMutator());
            
            

            unicastBus = new UnicastBus
            {
                MasterNodeAddress = MasterNodeAddress,
                TimeoutManagerAddress = MasterNodeAddress.SubScope("Timeouts"),
                MessageSerializer = MessageSerializer,
                Builder = FuncBuilder,
                MessageSender = messageSender,
                Transport = Transport,
                SubscriptionStorage = subscriptionStorage,
                AutoSubscribe = true,
                MessageMapper = MessageMapper,
                FailureManager = MockRepository.GenerateStub<IManageMessageFailures>()
            };
            bus = unicastBus;

            FuncBuilder.Register<IMutateOutgoingTransportMessages>(() => new CorrelationIdMutator{Bus = bus});
            FuncBuilder.Register<IBus>(() => bus);
            
            ExtensionMethods.SetHeaderAction = headerManager.SetHeader;

        }

        protected void VerifyThatMessageWasSentTo(Address destination)
        {
            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<Address>.Is.Equal(destination)));
        }

        protected void VerifyThatMessageWasSentWithHeaders(Func<IDictionary<string,string>,bool> predicate)
        {
            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(t => predicate(t.Headers)), Arg<Address>.Is.Anything));
        }

        protected void RegisterUow(IManageUnitsOfWork uow)
        {
            FuncBuilder.Register<IManageUnitsOfWork>(() => uow);
        }


        protected void RegisterMessageHandlerType<T>() where T : new()
        {
            FuncBuilder.Register<T>(()=> new T());
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
        public new void SetUp()
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