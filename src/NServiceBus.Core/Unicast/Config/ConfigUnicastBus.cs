namespace NServiceBus.Unicast.Config
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Features;
    using Logging;
    using Messages;
    using NServiceBus.Config;
    using ObjectBuilder;
    using Routing;
    using Settings;

    /// <summary>
    /// Inherits NServiceBus.Configure providing UnicastBus specific configuration on top of it.
    /// </summary>
    public class ConfigUnicastBus : Configure
    {
        /// <summary>
        /// Wrap the given configure object storing its builder and configurer.
        /// </summary>
        /// <param name="config"></param>
        public void Configure(Configure config)
        {
            Builder = config.Builder;
            Configurer = config.Configurer;
            busConfig = Configurer.ConfigureComponent<UnicastBus>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.MasterNodeAddress, config.GetMasterNodeAddress());

            var knownMessages = TypesToScan
                .Where(MessageConventionExtensions.IsMessageType)
                .ToList();

            ConfigureSubscriptionAuthorization();

            RegisterMessageModules();


            RegisterMessageOwnersAndBusAddress(knownMessages);

          
            ConfigureMessageRegistry(knownMessages);
        }

        void ConfigureMessageRegistry(List<Type> knownMessages)
        {
            var messageRegistry = new MessageMetadataRegistry
                {
                    DefaultToNonPersistentMessages = !SettingsHolder.Get<bool>("Endpoint.DurableMessages")
                };

            knownMessages.ForEach(messageRegistry.RegisterMessageType);

            Configurer.RegisterSingleton<MessageMetadataRegistry>(messageRegistry);
            
            if(!Logger.IsInfoEnabled)
                return;
            
            var messageDefinitions = messageRegistry.GetAllMessages().ToList();

            Logger.InfoFormat("Number of messages found: {0}" , messageDefinitions.Count());

            if (!Logger.IsDebugEnabled)
                return;


            Logger.DebugFormat("Message definitions: \n {0}",string.Concat(messageDefinitions.Select(md => md.ToString() + "\n")));
        }

#pragma warning disable 0618
        void RegisterMessageModules()
        {
            TypesToScan
                .Where(t => typeof(IMessageModule).IsAssignableFrom(t) && !t.IsInterface)
                .ToList()
                .ForEach(type => Configurer.ConfigureComponent(type, DependencyLifecycle.InstancePerCall));
        }
#pragma warning restore 0618

        void ConfigureSubscriptionAuthorization()
        {
            var authType = TypesToScan.FirstOrDefault(t => typeof(IAuthorizeSubscriptions).IsAssignableFrom(t) && !t.IsInterface);

            if (authType != null)
                Configurer.ConfigureComponent(authType, DependencyLifecycle.SingleInstance);
        }


        void RegisterMessageOwnersAndBusAddress(IEnumerable<Type> knownMessages)
        {
            var unicastConfig = GetConfigSection<UnicastBusConfig>();
            var router = new StaticMessageRouter(knownMessages);

            Configurer.RegisterSingleton<IRouteMessages>(router);

            Address forwardAddress = null;
            if (unicastConfig != null && !string.IsNullOrWhiteSpace(unicastConfig.ForwardReceivedMessagesTo))
            {
                forwardAddress = Address.Parse(unicastConfig.ForwardReceivedMessagesTo);
            }
            
            if (forwardAddress != null)
            {
                busConfig.ConfigureProperty(b => b.ForwardReceivedMessagesTo, forwardAddress);
            }

            if (unicastConfig == null)
            {
                return;
            }

            busConfig.ConfigureProperty(b => b.TimeToBeReceivedOnForwardedMessages, unicastConfig.TimeToBeReceivedOnForwardedMessages);

            var messageEndpointMappings = unicastConfig.MessageEndpointMappings.Cast<MessageEndpointMapping>()
                .OrderByDescending(m=>m)
                .ToList();

            foreach (var mapping in messageEndpointMappings)
            {
                mapping.Configure((messageType, address) =>
                    {
                        if (!MessageConventionExtensions.IsMessageType(messageType))
                        {
                            return;
                        }

                        router.RegisterRoute(messageType,address);
                    });
            }
        }
        
       
        /// <summary>
        /// Used to configure the bus.
        /// </summary>
        IComponentConfig<UnicastBus> busConfig;

        /// <summary>
        /// 
        /// Loads all message handler assemblies in the runtime directory.
        /// </summary>
        /// <returns></returns>
        public ConfigUnicastBus LoadMessageHandlers()
        {
            var types = new List<Type>();

            TypesToScan.Where(TypeSpecifiesMessageHandlerOrdering)
                .ToList().ForEach(t =>
                {
                    Logger.DebugFormat("Going to ask for message handler ordering from {0}.", t);

                    var order = new Order();
                    ((ISpecifyMessageHandlerOrdering)Activator.CreateInstance(t)).SpecifyOrder(order);

                    order.Types.ToList().ForEach(ht =>
                                      {
                                          if (types.Contains(ht))
                                              throw new ConfigurationErrorsException(string.Format("The order in which the type {0} should be invoked was already specified by a previous implementor of ISpecifyMessageHandlerOrdering. Check the debug logs to see which other specifiers have been invoked.", ht));
                                      });

                    types.AddRange(order.Types);
                });

            return LoadMessageHandlers(types);
        }

        /// <summary>
        /// Loads all message handler assemblies in the runtime directory
        /// and specifies that handlers in the given assembly should run
        /// before all others.
        /// 
        /// Use First{T} to indicate the type to load from.
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        /// <returns></returns>
        public ConfigUnicastBus LoadMessageHandlers<TFirst>()
        {
            var args = typeof(TFirst).GetGenericArguments();
            if (args.Length == 1)
            {
                if (typeof(First<>).MakeGenericType(args[0]).IsAssignableFrom(typeof(TFirst)))
                {
                    return LoadMessageHandlers(new[] { args[0] });
                }
            }

            throw new ArgumentException("TFirst should be of the type First<T> where T is the type to indicate as first.");
        }

        /// <summary>
        /// Loads all message handler assemblies in the runtime directory
        /// and specifies that the handlers in the given 'order' are to 
        /// run before all others and in the order specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="order"></param>
        /// <returns></returns>
        public ConfigUnicastBus LoadMessageHandlers<T>(First<T> order)
        {
            return LoadMessageHandlers(order.Types);
        }

        ConfigUnicastBus LoadMessageHandlers(IEnumerable<Type> orderedTypes)
        {
            LoadMessageHandlersCalled = true;
            var types = new List<Type>(TypesToScan);

            foreach (var t in orderedTypes)
                types.Remove(t);

            types.InsertRange(0, orderedTypes);

            return ConfigureMessageHandlersIn(types);
        }

        /// <summary>
        /// Scans the given types for types that are message handlers
        /// then uses the Configurer to configure them into the container as single call components,
        /// finally passing them to the bus as its MessageHandlerTypes.
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        ConfigUnicastBus ConfigureMessageHandlersIn(IEnumerable<Type> types)
        {
            var handlerRegistry = new MessageHandlerRegistry();
            var handlers = new List<Type>();

            foreach (var t in types.Where(IsMessageHandler))
            {
                Configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerUnitOfWork);
                handlerRegistry.RegisterHandler(t);
                handlers.Add(t);
            }

            Configurer.RegisterSingleton<IMessageHandlerRegistry>(handlerRegistry);

            var availableDispatcherFactories = TypesToScan
              .Where(
                  factory =>
                  !factory.IsInterface && typeof(IMessageDispatcherFactory).IsAssignableFrom(factory))
              .ToList();

            var dispatcherMappings = GetDispatcherFactories(handlers, availableDispatcherFactories);

            //configure the message dispatcher for each handler
            busConfig.ConfigureProperty(b => b.MessageDispatcherMappings, dispatcherMappings);

            availableDispatcherFactories.ToList().ForEach(factory => Configurer.ConfigureComponent(factory, DependencyLifecycle.InstancePerUnitOfWork));

            return this;
        }

        IDictionary<Type, Type> GetDispatcherFactories(IEnumerable<Type> handlers, IEnumerable<Type> messageDispatcherFactories)
        {
            var result = new Dictionary<Type, Type>();

            var customFactories = messageDispatcherFactories
                .Where(t => t != defaultDispatcherFactory)
                .Select(t => (IMessageDispatcherFactory)Activator.CreateInstance(t)).ToList();


            foreach (var handler in handlers)
            {
                var factory = customFactories.FirstOrDefault(f => f.CanDispatch(handler));

                var factoryTypeToUse = defaultDispatcherFactory;

                if (factory != null)
                    factoryTypeToUse = factory.GetType();

                result.Add(handler, factoryTypeToUse);
            }
            return result;
        }

        /// <summary>
        /// Set this if you want this endpoint to serve as something of a proxy;
        /// recipients of messages sent by this endpoint will see the address
        /// of endpoints that sent the incoming messages.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "PropagateReturnAddressOnSend")]
        public ConfigUnicastBus PropogateReturnAddressOnSend(bool value)
        {
            busConfig.ConfigureProperty(b => b.PropagateReturnAddressOnSend, value);
            return this;
        }
        
        /// <summary>
        /// Set this if you want this endpoint to serve as something of a proxy;
        /// recipients of messages sent by this endpoint will see the address
        /// of endpoints that sent the incoming messages.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ConfigUnicastBus PropagateReturnAddressOnSend(bool value)
        {
            busConfig.ConfigureProperty(b => b.PropagateReturnAddressOnSend, value);
            return this;
        }
        /// <summary>
        /// Forwards all received messages to a given endpoint (queue@machine).
        /// This is useful as an auditing/debugging mechanism.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ConfigUnicastBus ForwardReceivedMessagesTo(string value)
        {
            busConfig.ConfigureProperty(b => b.ForwardReceivedMessagesTo, value);
            return this;
        }

        /// <summary>
        /// Instructs the bus not to automatically subscribe to messages that
        /// it has handlers for (given those messages belong to a different endpoint).
        /// 
        /// This is needed only if you require fine-grained control over the subscribe/unsubscribe process.
        /// </summary>
        /// <returns></returns>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "Configure.Features.Disable<AutoSubscribe>()")]
        public ConfigUnicastBus DoNotAutoSubscribe()
        {
            Features.Disable<AutoSubscribe>();

            return this;
        }

        /// <summary>
        /// Instructs the bus not to automatically subscribe sagas to messages that
        /// it has handlers for (given those messages belong to a different endpoint).
        /// 
        /// This is needed only if you require fine-grained control over the subscribe/unsubscribe process.
        /// </summary>
        /// <returns></returns>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "Configure.Features.AutoSubscribe(f=>f.DoNotAutoSubscribeSagas())")]
        public ConfigUnicastBus DoNotAutoSubscribeSagas()
        {
            Features.AutoSubscribe(f => f.DoNotAutoSubscribeSagas());
            //ApplyDefaultAutoSubscriptionStrategy.DoNotAutoSubscribeSagas = true;
            return this;
        }
       
        /// <summary>
        /// Allow the bus to subscribe to itself
        /// </summary>
        /// <returns></returns>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "Configure.Features.AutoSubscribe(f=>f.DoNotRequireExplicitRouting())")]
        public ConfigUnicastBus AllowSubscribeToSelf()
        {
            Features.AutoSubscribe(f => f.DoNotRequireExplicitRouting());
            return this;
        }

        /// <summary>
        /// Tells the bus to auto subscribe plain messages in addition to events
        /// Commands will NOT be auto subscribed
        /// </summary>
        /// <returns></returns>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "Configure.Features.AutoSubscribe(f=>f.AutoSubscribePlainMessages())")]
        public ConfigUnicastBus AutoSubscribePlainMessages()
        {
            Features.AutoSubscribe(f => f.AutoSubscribePlainMessages());
            return this;
        }

        /// <summary>
        /// Causes the bus to not deserialize incoming messages. This means that no handlers are called and 
        /// you need to be subscribed to the ITransport.TransportMessageReceived event to handle the messages
        /// your self.
        /// </summary>
        /// <returns></returns>
        public ConfigUnicastBus SkipDeserialization()
        {
            busConfig.ConfigureProperty(b => b.SkipDeserialization, true);
            return this;
        }

        /// <summary>
        /// Allow the bus to subscribe to itself
        /// </summary>
        /// <returns></returns>
        public ConfigUnicastBus DefaultDispatcherFactory<T>() where T : IMessageDispatcherFactory
        {
            defaultDispatcherFactory = typeof(T);
            return this;
        }

        Type defaultDispatcherFactory = typeof(DefaultDispatcherFactory);

        /// <summary>
        /// Returns true if the given type is a message handler.
        /// </summary>
        /// <param name="t"></param>
        internal static bool IsMessageHandler(Type t)
        {
            if (t.IsAbstract || t.IsGenericType)
                return false;

            return t.GetInterfaces().Select(GetMessageTypeFromMessageHandler).Any(messageType => messageType != null);
        }

        static bool TypeSpecifiesMessageHandlerOrdering(Type t)
        {
            return typeof(ISpecifyMessageHandlerOrdering).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface;
        }

        /// <summary>
        /// Returns the message type handled by the given message handler type.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        static Type GetMessageTypeFromMessageHandler(Type t)
        {
            if (t.IsGenericType)
            {
                var args = t.GetGenericArguments();
                if (args.Length != 1)
                    return null;

                var handlerType = typeof(IHandleMessages<>).MakeGenericType(args[0]);
                if (handlerType.IsAssignableFrom(t))
                    return args[0];
            }

            return null;
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(UnicastBus));
        internal bool LoadMessageHandlersCalled { get; private set; }
    }
}
