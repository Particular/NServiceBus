using System;
using System.Configuration;
using NServiceBus.Logging;
using NServiceBus.ObjectBuilder;
using NServiceBus.Config;
using System.Collections.Generic;
using System.Linq;

namespace NServiceBus.Unicast.Config
{
    using System.Reflection;

    /// <summary>
    /// Inherits NServiceBus.Configure providing UnicastBus specific configuration on top of it.
    /// </summary>
    public class ConfigUnicastBus : Configure
    {
        /// <summary>
        /// A map of which message types (belonging to the given assemblies) are owned 
        /// by which endpoint.
        /// </summary>
        readonly IDictionary<Type, Address> typesToEndpoints = new Dictionary<Type, Address>();

        /// <summary>
        /// Wrap the given configure object storing its builder and configurer.
        /// </summary>
        /// <param name="config"></param>
        public void Configure(Configure config)
        {
            Builder = config.Builder;
            Configurer = config.Configurer;
            busConfig = Configurer.ConfigureComponent<UnicastBus>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.MasterNodeAddress, config.GetMasterNodeAddress())
                .ConfigureProperty(p => p.TimeoutManagerAddress, config.GetTimeoutManagerAddress())
                .ConfigureProperty(p => p.MaxThroughputPerSecond, LicenseConfig.GetMaxThroughputPerSecond());

            ConfigureSubscriptionAuthorization();

            RegisterMessageModules();

            RegisterLocalMessages();

            RegisterMessageOwnersAndBusAddress();
        }

        void RegisterMessageModules()
        {
            TypesToScan
                .Where(t => typeof(IMessageModule).IsAssignableFrom(t) && !t.IsInterface)
                .ToList()
                .ForEach(type => Configurer.ConfigureComponent(type, DependencyLifecycle.InstancePerCall));
        }

        void ConfigureSubscriptionAuthorization()
        {
            var authType = TypesToScan.FirstOrDefault(t => typeof(IAuthorizeSubscriptions).IsAssignableFrom(t) && !t.IsInterface);

            if (authType != null)
                Configurer.ConfigureComponent(authType, DependencyLifecycle.SingleInstance);
        }

        void RegisterLocalMessages()
        {
            TypesToScan
                .Where(t => t.IsMessageType())
                .ToList()
                .ForEach(t => typesToEndpoints[t] = Address.Undefined);
        }

        void RegisterMessageOwnersAndBusAddress()
        {
            var unicastBusConfig = GetConfigSection<UnicastBusConfig>();
            ConfigureBusProperties(unicastBusConfig);
        }

        void ConfigureBusProperties(UnicastBusConfig unicastConfig)
        {
            if (unicastConfig != null)
            {
                busConfig.ConfigureProperty(b => b.ForwardReceivedMessagesTo, !string.IsNullOrWhiteSpace(unicastConfig.ForwardReceivedMessagesTo) ? Address.Parse(unicastConfig.ForwardReceivedMessagesTo) : Address.Undefined);
                busConfig.ConfigureProperty(b => b.TimeToBeReceivedOnForwardedMessages, unicastConfig.TimeToBeReceivedOnForwardedMessages);

                foreach (MessageEndpointMapping mapping in unicastConfig.MessageEndpointMappings)
                {
                    try
                    {
                        var messageType = Type.GetType(mapping.Messages, false);
                        if (messageType != null)
                        {
                            typesToEndpoints[messageType] = Address.Parse(mapping.Endpoint);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Problem loading message type: " + mapping.Messages, ex);
                    }

                    var split = mapping.Messages.Split(new[] { ".*," }, StringSplitOptions.RemoveEmptyEntries);

                    string assemblyName;
                    string ns;

                    switch (split.Length)
                    {
                        case 1:
                            ns = null;
                            assemblyName = mapping.Messages;
                            break;
                        case 2:
                            ns = split[0].Trim();
                            assemblyName = split[1].Trim();
                            break;
                        default:
                            throw new ArgumentException("Message mapping configuration is invalid: " + mapping.Messages);
                    }

                    try
                    {
                        var a = Assembly.Load(assemblyName);
                        var messageTypes = a.GetTypes().AsQueryable();

                        if (!string.IsNullOrEmpty(ns))
                            messageTypes = messageTypes.Where(t => !string.IsNullOrEmpty(t.Namespace) && t.Namespace.Equals(ns, StringComparison.InvariantCultureIgnoreCase));

                        foreach (var t in messageTypes)
                            typesToEndpoints[t] = Address.Parse(mapping.Endpoint);
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException("Problem loading message assembly: " + mapping.Messages, ex);
                    }
                }
            }

            busConfig.ConfigureProperty(b => b.MessageOwners, typesToEndpoints);
        }



        /// <summary>
        /// Used to configure the bus.
        /// </summary>
        IComponentConfig<UnicastBus> busConfig;

        /// <summary>
        /// Setting throttling message receiving rate, per receiving thread, measured in messages per second.
        /// </summary>
        /// <param name="messagesPerSecond"></param>
        /// <returns></returns>
        public ConfigUnicastBus DecreaseThroughputTo(int messagesPerSecond)
        {
            var licenseMaxThroughputPerSecond = LicenseConfig.GetMaxThroughputPerSecond();
            if ((licenseMaxThroughputPerSecond == 0) || (messagesPerSecond < licenseMaxThroughputPerSecond))
            {
                busConfig.ConfigureProperty(b => b.MaxThroughputPerSecond, messagesPerSecond);
                Logger.InfoFormat("Message receiving throughput was decreased to: [{0}] message per second", messagesPerSecond);
                return this;
            }

            Logger.WarnFormat("Attempt to decrease your max message throughput to a value higher than [{0}], which is specified in your license, is not allowed.", licenseMaxThroughputPerSecond);
            return this;
        }
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
            Type[] args = typeof(TFirst).GetGenericArguments();
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

            foreach (Type t in orderedTypes)
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
            var handlers = new List<Type>();

            foreach (Type t in types.Where(IsMessageHandler))
            {
                Configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall);
                handlers.Add(t);
            }

            busConfig.ConfigureProperty(b => b.MessageHandlerTypes, handlers);


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
        public ConfigUnicastBus PropogateReturnAddressOnSend(bool value)
        {
            busConfig.ConfigureProperty(b => b.PropogateReturnAddressOnSend, value);
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
        public ConfigUnicastBus DoNotAutoSubscribe()
        {
            busConfig.ConfigureProperty(b => b.AutoSubscribe, false);
            return this;
        }

        /// <summary>
        /// Instructs the bus not to automatically subscribe sagas to messages that
        /// it has handlers for (given those messages belong to a different endpoint).
        /// 
        /// This is needed only if you require fine-grained control over the subscribe/unsubscribe process.
        /// </summary>
        /// <returns></returns>
        public ConfigUnicastBus DoNotAutoSubscribeSagas()
        {
            busConfig.ConfigureProperty(b => b.DoNotAutoSubscribeSagas, true);
            return this;
        }
        /// <summary>
        /// Allow the bus to subscribe to itself
        /// </summary>
        /// <returns></returns>
        public ConfigUnicastBus AllowSubscribeToSelf()
        {
            busConfig.ConfigureProperty(b => b.AllowSubscribeToSelf, true);
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
        /// <returns></returns>
        static bool IsMessageHandler(Type t)
        {
            if (t.IsAbstract)
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
                Type[] args = t.GetGenericArguments();
                if (args.Length != 1)
                    return null;

                Type handlerType = typeof(IMessageHandler<>).MakeGenericType(args[0]);
                if (handlerType.IsAssignableFrom(t))
                    return args[0];
            }

            return null;
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(UnicastBus));
        internal bool LoadMessageHandlersCalled { get; private set; }
    }
}
