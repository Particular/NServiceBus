using System;
using System.Reflection;
using ObjectBuilder;
using NServiceBus.Saga;

namespace NServiceBus.Config
{
    public class Configure
    {
        private IBuilder builder;

        public static Configure With(IBuilder b)
        {
            Configure result = new Configure();
            result.builder = b;

            return result;
        }

        public void SagasAndMessageHandlersIn(params Assembly[] assemblies)
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
