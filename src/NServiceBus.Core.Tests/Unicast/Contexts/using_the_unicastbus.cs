namespace NServiceBus.Unicast.Tests.Contexts
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;
    using Audit;
    using Core.Tests;
    using Helpers;
    using Impersonation;
    using Impersonation.Windows;
    using MessageInterfaces.MessageMapper.Reflection;
    using MessageMutator;
    using Monitoring;
    using NUnit.Framework;
    using Pipeline.Behaviors;
    using Publishing;
    using Rhino.Mocks;
    using Routing;
    using Sagas;
    using Serializers.XML;
    using Settings;
    using Subscriptions.MessageDrivenSubscriptions;
    using Subscriptions.MessageDrivenSubscriptions.SubcriberSideFiltering;
    using Timeout;
    using Transports;
    using Unicast.Messages;
    using UnitOfWork;

    public class using_a_configured_unicastBus
    {
        protected UnicastBus bus;

        protected UnicastBus unicastBus;
        protected ISendMessages messageSender;
        protected FakeSubscriptionStorage subscriptionStorage;

        protected Address gatewayAddress;
        MessageHeaderManager headerManager = new MessageHeaderManager();
        MessageMapper MessageMapper = new MessageMapper();

        protected FakeTransport Transport;
        protected XmlMessageSerializer MessageSerializer;
        protected FuncBuilder FuncBuilder;
        protected Address MasterNodeAddress;
        protected EstimatedTimeToSLABreachCalculator SLABreachCalculator = new EstimatedTimeToSLABreachCalculator();
        protected MessageMetadataRegistry MessageMetadataRegistry;
        protected MessageDrivenSubscriptionManager subscriptionManager;
        SubscriptionPredicatesEvaluator subscriptionPredicatesEvaluator;
        protected StaticMessageRouter router;

        protected MessageHandlerRegistry handlerRegistry;
        protected FakeMessageAuditer fakeMessageAuditer;


        [SetUp]
        public void SetUp()
        {
            HandlerInvocationCache.Clear();

            SettingsHolder.Reset();
            SettingsHolder.SetDefault("Endpoint.SendOnly", false);

            Transport = new FakeTransport();
            FuncBuilder = new FuncBuilder();
            Configure.GetEndpointNameAction = () => "TestEndpoint";
            const string localAddress = "endpointA";
            MasterNodeAddress = new Address(localAddress, "MasterNode");
            subscriptionPredicatesEvaluator = new SubscriptionPredicatesEvaluator();
            router = new StaticMessageRouter(KnownMessageTypes());
            handlerRegistry = new MessageHandlerRegistry();
            MessageMetadataRegistry = new MessageMetadataRegistry
                {
                    DefaultToNonPersistentMessages = false
                };

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
            fakeMessageAuditer = new FakeMessageAuditer();
            subscriptionManager = new MessageDrivenSubscriptionManager
                {
                    Builder = FuncBuilder,
                    MessageSender = messageSender,
                    SubscriptionStorage = subscriptionStorage
                };

            FuncBuilder.Register<IMutateOutgoingTransportMessages>(() => headerManager);
            FuncBuilder.Register<IMutateIncomingMessages>(() => new FilteringMutator
                {
                    SubscriptionPredicatesEvaluator = subscriptionPredicatesEvaluator
                });
            FuncBuilder.Register<IMutateOutgoingTransportMessages>(() => new SentTimeMutator());
            FuncBuilder.Register<IMutateIncomingTransportMessages>(() => subscriptionManager);
            FuncBuilder.Register<DefaultDispatcherFactory>(() => new DefaultDispatcherFactory());
            FuncBuilder.Register<EstimatedTimeToSLABreachCalculator>(() => SLABreachCalculator);
            FuncBuilder.Register<IMessageHandlerRegistry>(() => handlerRegistry);
            FuncBuilder.Register<ExtractIncomingPrincipal>(() => new WindowsImpersonator());

            FuncBuilder.Register<UnitOfWorkBehavior>();
            FuncBuilder.Register<MessageHandlingLoggingBehavior>();
            FuncBuilder.Register<ExtractLogicalMessagesBehavior>(() => new ExtractLogicalMessagesBehavior
                                                             {
                                                                 MessageSerializer = MessageSerializer,
                                                                 MessageMetadataRegistry = MessageMetadataRegistry,
                                                             });
            FuncBuilder.Register<ApplyIncomingMessageMutatorsBehavior>();
            FuncBuilder.Register<ImpersonateSenderBehavior>(() => new ImpersonateSenderBehavior
                {
                    ExtractIncomingPrincipal = MockRepository.GenerateStub<ExtractIncomingPrincipal>()
                });
            FuncBuilder.Register<LoadHandlersBehavior>();
            FuncBuilder.Register<SagaPersistenceBehavior>();
            FuncBuilder.Register<InvokeHandlersBehavior>();

            FuncBuilder.Register<CallbackInvocationBehavior>(() => new CallbackInvocationBehavior());
            FuncBuilder.Register<ApplyIncomingTransportMessageMutatorsBehavior>();

            unicastBus = new UnicastBus
            {
                MasterNodeAddress = MasterNodeAddress,
                MessageSerializer = MessageSerializer,
                Builder = FuncBuilder,
                MessageSender = messageSender,
                Transport = Transport,
                MessageMapper = MessageMapper,
                MessagePublisher = new StorageDrivenPublisher
                    {
                        MessageSender = messageSender,
                        SubscriptionStorage = subscriptionStorage
                    },
                MessageDeferrer = new TimeoutManagerDeferrer
                    {
                        MessageSender = messageSender,
                        TimeoutManagerAddress = MasterNodeAddress.SubScope("Timeouts")
                    },
                SubscriptionManager = subscriptionManager,
                MessageMetadataRegistry = MessageMetadataRegistry,
                SubscriptionPredicatesEvaluator = subscriptionPredicatesEvaluator,
                MessageRouter = router,
                MessageAuditer = fakeMessageAuditer
            };
            bus = unicastBus;

            FuncBuilder.Register<IMutateOutgoingTransportMessages>(() => new CausationMutator { Bus = bus });
            FuncBuilder.Register<IBus>(() => bus);
            FuncBuilder.Register<UnicastBus>(() => unicastBus);

            ExtensionMethods.SetHeaderAction = headerManager.SetHeader;
        }

        protected virtual IEnumerable<Type> KnownMessageTypes()
        {
            return new Collection<Type>();
        }

        protected void VerifyThatMessageWasSentTo(Address destination)
        {
            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<Address>.Is.Equal(destination)));
        }

        protected void VerifyThatMessageWasSentWithHeaders(Func<IDictionary<string, string>, bool> predicate)
        {
            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(t => predicate(t.Headers)), Arg<Address>.Is.Anything));
        }

        protected void RegisterUow(IManageUnitsOfWork uow)
        {
            FuncBuilder.Register<IManageUnitsOfWork>(() => uow);
        }

        protected void RegisterMessageHandlerType<T>() where T : new()
        {
            FuncBuilder.Register<T>(() => new T());

            handlerRegistry.RegisterHandler(typeof(T));

            if (unicastBus.MessageDispatcherMappings == null)
                unicastBus.MessageDispatcherMappings = new Dictionary<Type, Type>();
        }
        protected void RegisterOwnedMessageType<T>()
        {
            router.RegisterRoute(typeof(T), Address.Local);
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
            router.RegisterRoute(typeof(T), address);
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
                  x.Send(Arg<TransportMessage>.Matches(m => condition(m)), Arg<Address>.Is.Equal(addressOfPublishingEndpoint)));

            }
            catch (Exception)
            {
                //retry to avoid race conditions 
                Thread.Sleep(2000);
                messageSender.AssertWasCalled(x =>
                 x.Send(Arg<TransportMessage>.Matches(m => condition(m)), Arg<Address>.Is.Equal(addressOfPublishingEndpoint)));
            }
        }

        protected void AssertSubscription<T>(Address addressOfPublishingEndpoint)
        {
            try
            {
                messageSender.AssertWasCalled(x =>
                  x.Send(Arg<TransportMessage>.Matches(m => IsSubscriptionFor<T>(m)), Arg<Address>.Is.Equal(addressOfPublishingEndpoint)));

            }
            catch (Exception)
            {
                //retry to avoid race conditions 
                Thread.Sleep(1000);
                messageSender.AssertWasCalled(x =>
                  x.Send(Arg<TransportMessage>.Matches(m => IsSubscriptionFor<T>(m)), Arg<Address>.Is.Equal(addressOfPublishingEndpoint)));
            }
        }

        bool IsSubscriptionFor<T>(TransportMessage transportMessage)
        {
            var type = Type.GetType(transportMessage.Headers[Headers.SubscriptionMessageType]);

            return type == typeof(T);
        }
    }

    public class FakeMessageAuditer : MessageAuditer
    {
        public override void ForwardMessageToAuditQueue(TransportMessage transportMessage)
        {
        }
    }

    public class using_the_unicastBus : using_a_configured_unicastBus
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
                ExtensionMethods.GetHeaderAction = (o, s) =>
                {
                    string v;
                    transportMessage.Headers.TryGetValue(s, out v);
                    return v;
                };

                ExtensionMethods.SetHeaderAction = (o, s, v) => { transportMessage.Headers[s] = v; };

                Transport.FakeMessageBeingProcessed(transportMessage);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Fake message processing failed: " + ex);
                ResultingException = ex;
            }
        }

        protected void ReceiveMessage<T>(T message, IDictionary<string,string> headers = null)
        {
            RegisterMessageType<T>();
            var messageToReceive = Helpers.Serialize(message);

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
