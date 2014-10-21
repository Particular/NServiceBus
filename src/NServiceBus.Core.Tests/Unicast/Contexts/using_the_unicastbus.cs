namespace NServiceBus.Unicast.Tests.Contexts
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Runtime.Serialization;
    using System.Threading;
    using Core.Tests;
    using Helpers;
    using Licensing;
    using MessageInterfaces;
    using MessageInterfaces.MessageMapper.Reflection;
    using MessageMutator;
    using Monitoring;
    using NServiceBus.Hosting;
    using NServiceBus.ObjectBuilder;
    using NUnit.Framework;
    using Pipeline;
    using Publishing;
    using Rhino.Mocks;
    using Routing;
    using Serialization;
    using Serializers.XML;
    using Settings;
    using Subscriptions.MessageDrivenSubscriptions;
    using Timeout;
    using Transports;
    using Unicast.Messages;
    using UnitOfWork;

    class using_a_configured_unicastBus
    {
        protected UnicastBus bus;

        protected ISendMessages messageSender;
        protected FakeSubscriptionStorage subscriptionStorage;

        protected MessageMapper MessageMapper = new MessageMapper();

        protected FakeTransport Transport;
        protected XmlMessageSerializer MessageSerializer;
        protected FuncBuilder FuncBuilder;
        public static Address MasterNodeAddress;
        protected EstimatedTimeToSLABreachCalculator SLABreachCalculator = (EstimatedTimeToSLABreachCalculator) FormatterServices.GetUninitializedObject(typeof(EstimatedTimeToSLABreachCalculator));
        protected MessageMetadataRegistry MessageMetadataRegistry;
        protected SubscriptionManager subscriptionManager;
        protected StaticMessageRouter router;

        protected MessageHandlerRegistry handlerRegistry;
        protected TransportDefinition transportDefinition;
        protected SettingsHolder settings;
        protected Configure configure;
        protected PipelineModifications pipelineModifications;

        PipelineExecutor pipelineFactory;

        static using_a_configured_unicastBus()
        {
            var localAddress = "endpointA";
            MasterNodeAddress = new Address(localAddress, "MasterNode");
        }

        [SetUp]
        public void SetUp()
        {
            LicenseManager.InitializeLicense();
            transportDefinition = new MsmqTransport();
            
            settings = new SettingsHolder();

            settings.SetDefault("EndpointName", "TestEndpoint");
            settings.SetDefault("Endpoint.SendOnly", false);
            settings.SetDefault("MasterNode.Address", MasterNodeAddress);
            pipelineModifications = new PipelineModifications();
            settings.Set<PipelineModifications>(pipelineModifications);

            ApplyPipelineModifications();

            Transport = new FakeTransport();
            FuncBuilder = new FuncBuilder();

            FuncBuilder.Register<ReadOnlySettings>(() => settings);

            router = new StaticMessageRouter(KnownMessageTypes());
            var conventions = new Conventions();
            handlerRegistry = new MessageHandlerRegistry(conventions);
            MessageMetadataRegistry = new MessageMetadataRegistry(false, conventions);
            MessageSerializer = new XmlMessageSerializer(MessageMapper, conventions);

            messageSender = MockRepository.GenerateStub<ISendMessages>();
            subscriptionStorage = new FakeSubscriptionStorage();
            configure = new Configure(settings, FuncBuilder, new List<Action<IConfigureComponents>>(), new PipelineSettings(null))
            {
                localAddress = Address.Parse("TestEndpoint")
            };

            subscriptionManager = new SubscriptionManager
                {
                    MessageSender = messageSender,
                    SubscriptionStorage = subscriptionStorage,
                    Configure = configure
                };

            pipelineFactory = new PipelineExecutor(settings, FuncBuilder, new BusNotifications());

            FuncBuilder.Register<IMessageSerializer>(() => MessageSerializer);
            FuncBuilder.Register<ISendMessages>(() => messageSender);

            FuncBuilder.Register<LogicalMessageFactory>(() => new LogicalMessageFactory(MessageMetadataRegistry, MessageMapper, pipelineFactory));

            FuncBuilder.Register<IManageSubscriptions>(() => subscriptionManager);
            FuncBuilder.Register<EstimatedTimeToSLABreachCalculator>(() => SLABreachCalculator);
            FuncBuilder.Register<MessageMetadataRegistry>(() => MessageMetadataRegistry);

            FuncBuilder.Register<IMessageHandlerRegistry>(() => handlerRegistry);
            FuncBuilder.Register<IMessageMapper>(() => MessageMapper);

            FuncBuilder.Register<DeserializeLogicalMessagesBehavior>(() => new DeserializeLogicalMessagesBehavior
                                                             {
                                                                 MessageSerializer = MessageSerializer,
                                                                 MessageMetadataRegistry = MessageMetadataRegistry,
                                                             });

            FuncBuilder.Register<CreatePhysicalMessageBehavior>(() => new CreatePhysicalMessageBehavior());
            FuncBuilder.Register<PipelineExecutor>(() => pipelineFactory);
            FuncBuilder.Register<TransportDefinition>(() => transportDefinition);

            var messagePublisher = new StorageDrivenPublisher
            {
                MessageSender = messageSender,
                SubscriptionStorage = subscriptionStorage
            };

            var deferrer = new TimeoutManagerDeferrer
            {
                MessageSender = messageSender,
                TimeoutManagerAddress = MasterNodeAddress.SubScope("Timeouts"),
                Configure = configure,
            };

            FuncBuilder.Register<IDeferMessages>(() => deferrer);
            FuncBuilder.Register<IPublishMessages>(() => messagePublisher);

            bus = new UnicastBus
            {
                Builder = FuncBuilder,
                MessageSender = messageSender,
                Transport = Transport,
                MessageMapper = MessageMapper,
                SubscriptionManager = subscriptionManager,
                MessageRouter = router,
                Settings = settings,
                Configure = configure,
                HostInformation = new HostInformation(Guid.NewGuid(), "HelloWorld")
            };

            FuncBuilder.Register<IMutateOutgoingTransportMessages>(() => new CausationMutator { Bus = bus });
            FuncBuilder.Register<IBus>(() => bus);
            FuncBuilder.Register<UnicastBus>(() => bus);
            FuncBuilder.Register<Conventions>(() => conventions);
            FuncBuilder.Register<Configure>(() => configure);
        }

        protected virtual void ApplyPipelineModifications()
        {
        }

        protected virtual IEnumerable<Type> KnownMessageTypes()
        {
            return new Collection<Type>();
        }

        protected void VerifyThatMessageWasSentTo(Address destination)
        {
            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<SendOptions>.Matches(o => o.Destination == destination)));
        }

        protected void VerifyThatMessageWasSentWithHeaders(Func<IDictionary<string, string>, bool> predicate)
        {
            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(t => predicate(t.Headers)), Arg<SendOptions>.Is.Anything));
        }

        protected void RegisterUow(IManageUnitsOfWork uow)
        {
            FuncBuilder.Register<IManageUnitsOfWork>(() => uow);
        }

        protected void RegisterMessageHandlerType<T>() where T : new()
        {
// ReSharper disable HeapView.SlowDelegateCreation
            FuncBuilder.Register<T>(() => new T());
// ReSharper restore HeapView.SlowDelegateCreation

            handlerRegistry.RegisterHandler(typeof(T));
        }
        protected void RegisterOwnedMessageType<T>()
        {
            router.RegisterMessageRoute(typeof(T), configure.LocalAddress);
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
            router.RegisterMessageRoute(typeof(T), address);
            MessageMetadataRegistry.RegisterMessageType(typeof(T));

        }

        protected void StartBus()
        {
            ((IStartableBus)bus).Start();
        }

        protected void AssertSubscription(Predicate<TransportMessage> condition, Address addressOfPublishingEndpoint)
        {
            try
            {
                messageSender.AssertWasCalled(x =>
                  x.Send(Arg<TransportMessage>.Matches(m => condition(m)), Arg<SendOptions>.Matches(o => o.Destination == addressOfPublishingEndpoint)));

            }
            catch (Exception)
            {
                //retry to avoid race conditions 
                Thread.Sleep(2000);
                messageSender.AssertWasCalled(x =>
                 x.Send(Arg<TransportMessage>.Matches(m => condition(m)), Arg<SendOptions>.Matches(o => o.Destination == addressOfPublishingEndpoint)));
            }
        }

        protected void AssertSubscription<T>(Address addressOfPublishingEndpoint)
        {
            try
            {
                messageSender.AssertWasCalled(x =>
                  x.Send(Arg<TransportMessage>.Matches(m => IsSubscriptionFor<T>(m)), Arg<SendOptions>.Matches(o => o.Destination == addressOfPublishingEndpoint)));

            }
            catch (Exception)
            {
                //retry to avoid race conditions 
                Thread.Sleep(1000);
                messageSender.AssertWasCalled(x =>
                  x.Send(Arg<TransportMessage>.Matches(m => IsSubscriptionFor<T>(m)), Arg<SendOptions>.Matches(o => o.Destination == addressOfPublishingEndpoint)));
            }
        }

        bool IsSubscriptionFor<T>(TransportMessage transportMessage)
        {
            var type = Type.GetType(transportMessage.Headers[Headers.SubscriptionMessageType]);

            return type == typeof(T);
        }
    }


    class using_the_unicastBus : using_a_configured_unicastBus
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
                bus.GetHeaderAction = (o, s) =>
                {
                    string v;
                    transportMessage.Headers.TryGetValue(s, out v);
                    return v;
                };

                bus.SetHeaderAction = (o, s, v) => { transportMessage.Headers[s] = v; };

                Transport.FakeMessageBeingProcessed(transportMessage);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Fake message processing failed: " + ex);
                ResultingException = ex;
            }
        }

        protected void ReceiveMessage<T>(T message, IDictionary<string, string> headers = null, MessageMapper mapper = null)
        {
            RegisterMessageType<T>();
            var messageToReceive = Helpers.Serialize(message, mapper: mapper);

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    messageToReceive.Headers[header.Key] = header.Value;
                }
            }

            ReceiveMessage(messageToReceive);
        }

        protected void SimulateMessageBeingAbortedDueToRetryCountExceeded(TransportMessage transportMessage)
        {
            try
            {
                Transport.FakeMessageBeingPassedToTheFaultManager(transportMessage);
            }
            catch (Exception ex)
            {
                ResultingException = ex;
            }
        }
    }
}
