using System;
using NServiceBus.ObjectBuilder;
using System.Collections;
using System.Reflection;
using NServiceBus.Config;
using System.Collections.Generic;
using NServiceBus.Saga;

namespace NServiceBus.Unicast.Config
{
    /// <summary>
    /// Inherits NServiceBus.Configure providing UnicastBus specific configuration on top of it.
    /// </summary>
    public class ConfigUnicastBus : Configure
    {
        /// <summary>
        /// A map of which message types (belonging to the given assemblies) are owned 
        /// by which endpoint.
        /// </summary>
        protected Hashtable assembliesToEndpoints = new Hashtable();

        /// <summary>
        /// Wrap the given configure object storing its builder and configurer.
        /// </summary>
        /// <param name="config"></param>
        public void Configure(Configure config)
        {
            Builder = config.Builder;
            Configurer = config.Configurer;

            busConfig = Configurer.ConfigureComponent<UnicastBus>(ComponentCallModelEnum.Singleton);

            var cfg = GetConfigSection<UnicastBusConfig>();

            if (cfg != null)
            {
                foreach (Type t in TypesToScan)
                    if (typeof(IMessage).IsAssignableFrom(t))
                        assembliesToEndpoints[t.Assembly.GetName().Name] = string.Empty;

                foreach (MessageEndpointMapping mapping in cfg.MessageEndpointMappings)
                    assembliesToEndpoints[mapping.Messages] = mapping.Endpoint;

                busConfig.ConfigureProperty(b => b.DistributorControlAddress, cfg.DistributorControlAddress);
                busConfig.ConfigureProperty(b => b.DistributorDataAddress, cfg.DistributorDataAddress);
                busConfig.ConfigureProperty(b => b.ForwardReceivedMessagesTo, cfg.ForwardReceivedMessagesTo);
                busConfig.ConfigureProperty(b => b.MessageOwners, assembliesToEndpoints);
            }
        }

        /// <summary>
        /// Used to configure the bus.
        /// </summary>
        protected IComponentConfig<UnicastBus> busConfig;

        /// <summary>
        /// Instructs the bus to run the processing of messages being handled
        /// under the permissions of the sender of the message.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ConfigUnicastBus ImpersonateSender(bool value)
        {
            busConfig.ConfigureProperty(b => b.ImpersonateSender, value);
            return this;
        }

        /// <summary>
        /// [Deprecated] Use LoadMessageHandlers instead.
        /// </summary>
        /// <param name="assemblies"></param>
        /// <returns></returns>
        [Obsolete]
        public ConfigUnicastBus SetMessageHandlersFromAssembliesInOrder(params Assembly[] assemblies)
        {
            var types = new List<Type>();
            foreach (Assembly a in assemblies)
                types.AddRange(a.GetTypes());

            return ConfigureMessageHandlersIn(types);
        }

        /// <summary>
        /// Loads all message handler assemblies in the runtime directory.
        /// </summary>
        /// <returns></returns>
        public ConfigUnicastBus LoadMessageHandlers()
        {
            return ConfigureMessageHandlersIn(TypesToScan);
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
                    var types = new List<Type>(TypesToScan);

                    types.Remove(args[0]);
                    types.Insert(0, args[0]);

                    return ConfigureMessageHandlersIn(types);
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
            var types = new List<Type>(TypesToScan);

            foreach (Type t in order.Types)
                types.Remove(t);

            types.InsertRange(0, order.Types);

            return ConfigureMessageHandlersIn(types);
        }

        /// <summary>
        /// Scans the given types for types that are message handlers
        /// then uses the Configurer to configure them into the container as single call components,
        /// finally passing them to the bus as its MessageHandlerTypes.
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        protected ConfigUnicastBus ConfigureMessageHandlersIn(IEnumerable<Type> types)
        {
            var handlers = new List<Type>();

            foreach (Type t in types)
                if (IsMessageHandler(t))
                {
                    Configurer.ConfigureComponent(t, ComponentCallModelEnum.Singlecall);
                    handlers.Add(t);
                }

            busConfig.ConfigureProperty(b => b.MessageHandlerTypes, handlers);

            return this;
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
        public ConfigUnicastBus ForwardReceivedMessagesTo(string  value)
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
        /// Returns true if the given type is a message handler.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsMessageHandler(Type t)
        {
            if (t.IsAbstract)
                return false;

            if (typeof(ISaga).IsAssignableFrom(t))
                return false;

            foreach (Type interfaceType in t.GetInterfaces())
            {
                Type messageType = GetMessageTypeFromMessageHandler(interfaceType);
                if (messageType != null)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the message type handled by the given message handler type.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Type GetMessageTypeFromMessageHandler(Type t)
        {
            if (t.IsGenericType)
            {
                Type[] args = t.GetGenericArguments();
                if (args.Length != 1)
                    return null;

                if (!typeof(IMessage).IsAssignableFrom(args[0]))
                    return null;

                Type handlerType = typeof(IMessageHandler<>).MakeGenericType(args[0]);
                if (handlerType.IsAssignableFrom(t))
                    return args[0];
            }

            return null;
        }
    }
}
