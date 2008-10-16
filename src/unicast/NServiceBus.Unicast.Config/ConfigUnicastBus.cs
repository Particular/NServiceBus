using System;
using System.Configuration;
using NServiceBus.Saga;
using ObjectBuilder;
using System.Collections;
using System.Reflection;

namespace NServiceBus.Unicast.Config
{
    public class ConfigUnicastBus
    {
        public ConfigUnicastBus(IBuilder builder)
        {
            this.builder = builder;

            UnicastBusConfig cfg = ConfigurationManager.GetSection("UnicastBusConfig") as UnicastBusConfig;

            if (cfg == null)
                throw new ConfigurationErrorsException("Could not find configuration section for UnicastBus.");

            Hashtable hashtable = new Hashtable();

            foreach (MessageEndpointMapping mapping in cfg.MessageEndpointMappings)
                hashtable[mapping.Messages] = mapping.Endpoint;

            bus = builder.ConfigureComponent<UnicastBus>(ComponentCallModelEnum.Singleton);

            bus.DistributorControlAddress = cfg.DistributorControlAddress;
            bus.DistributorDataAddress = cfg.DistributorDataAddress;
            bus.MessageOwners = hashtable;
        }

        private readonly UnicastBus bus;
        private readonly IBuilder builder;

        public ConfigUnicastBus ImpersonateSender(bool value)
        {
            bus.ImpersonateSender = value;
            return this;
        }

        public ConfigUnicastBus SetMessageHandlersFromAssembliesInOrder(params Assembly[] assemblies)
        {
            ConfigureSagasAndMessageHandlersIn(assemblies);

            bus.MessageHandlerAssemblies = new ArrayList(assemblies);

            return this;
        }

        public ConfigUnicastBus PropogateReturnAddressOnSend(bool value)
        {
            bus.PropogateReturnAddressOnSend = value;
            return this;
        }

        public ConfigUnicastBus ForwardReceivedMessagesTo(string  value)
        {
            bus.ForwardReceivedMessagesTo = value;
            return this;
        }

        public ConfigUnicastBus DoNotAutoSubscribe()
        {
            bus.AutoSubscribe = false;
            return this;
        }

        private void ConfigureSagasAndMessageHandlersIn(params Assembly[] assemblies)
        {
            foreach (Assembly a in assemblies)
                foreach (Type t in a.GetTypes())
                {
                    if (t.IsInterface ||
                        t.IsAbstract ||
                        (!(typeof(ISagaEntity).IsAssignableFrom(t) || IsMessageHandler(t)))
                        )
                        continue;

                    builder.ConfigureComponent(t, ComponentCallModelEnum.Singlecall);
                }
        }

        public static bool IsMessageHandler(Type t)
        {
            if (t.IsAbstract)
                return false;

            Type parent = t.BaseType;
            while (parent != typeof(Object))
            {
                Type messageType = GetMessageTypeFromMessageHandler(parent);
                if (messageType != null)
                    return true;

                parent = parent.BaseType;
            }

            foreach (Type interfaceType in t.GetInterfaces())
            {
                Type messageType = GetMessageTypeFromMessageHandler(interfaceType);
                if (messageType != null)
                    return true;
            }

            return false;
        }

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
