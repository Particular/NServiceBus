using System;
using System.Configuration;
using NServiceBus.ObjectBuilder;
using System.Collections;
using System.Reflection;
using NServiceBus.Config;
using System.IO;

namespace NServiceBus.Unicast.Config
{
    /// <summary>
    /// Inherits NServiceBus.Configure providing UnicastBus specific configuration on top of it.
    /// </summary>
    public class ConfigUnicastBus : Configure
    {
        /// <summary>
        /// Just calls the base constructor (needed because we're providing another constructor).
        /// </summary>
        public ConfigUnicastBus() : base() {}

        /// <summary>
        /// Wrap the given configure object storing its builder and configurer.
        /// </summary>
        /// <param name="config"></param>
        public void Configure(Configure config)
        {
            this.Builder = config.Builder;
            this.Configurer = config.Configurer;

            UnicastBusConfig cfg = ConfigurationManager.GetSection("UnicastBusConfig") as UnicastBusConfig;

            if (cfg == null)
                throw new ConfigurationErrorsException("Could not find configuration section for UnicastBus.");

            Hashtable hashtable = new Hashtable();

            foreach (string dll in Directory.GetFiles(Environment.CurrentDirectory, "*.dll", SearchOption.AllDirectories))
                hashtable.Add(AssemblyName.GetAssemblyName(Path.GetFileName(dll)).Name, string.Empty);

            hashtable.Add(Assembly.GetEntryAssembly().GetName().Name, string.Empty);

            foreach (MessageEndpointMapping mapping in cfg.MessageEndpointMappings)
                hashtable[mapping.Messages] = mapping.Endpoint;

            bus = Configurer.ConfigureComponent<UnicastBus>(ComponentCallModelEnum.Singleton);

            bus.DistributorControlAddress = cfg.DistributorControlAddress;
            bus.DistributorDataAddress = cfg.DistributorDataAddress;
            bus.MessageOwners = hashtable;
        }

        private UnicastBus bus;

        /// <summary>
        /// Instructs the bus to run the processing of messages being handled
        /// under the permissions of the sender of the message.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ConfigUnicastBus ImpersonateSender(bool value)
        {
            bus.ImpersonateSender = value;
            return this;
        }

        /// <summary>
        /// Configures the order in which handlers should be run when processing messages.
        /// </summary>
        /// <param name="assemblies"></param>
        /// <returns></returns>
        public ConfigUnicastBus SetMessageHandlersFromAssembliesInOrder(params Assembly[] assemblies)
        {
            ConfigureSagasAndMessageHandlersIn(assemblies);

            bus.MessageHandlerAssemblies = new ArrayList(assemblies);

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
            bus.PropogateReturnAddressOnSend = value;
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
            bus.ForwardReceivedMessagesTo = value;
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
            bus.AutoSubscribe = false;
            return this;
        }

        private void ConfigureSagasAndMessageHandlersIn(params Assembly[] assemblies)
        {
            NServiceBus.Saga.Configure.With(this.Configurer, this.Builder).SagasInAssemblies(assemblies);

            foreach (Assembly a in assemblies)
                foreach (Type t in a.GetTypes())
                {
                    if (IsMessageHandler(t))
                        this.Configurer.ConfigureComponent(t, ComponentCallModelEnum.Singlecall);
                }
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
