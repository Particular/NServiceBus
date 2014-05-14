namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using Logging;
    using Messages;
    using ObjectBuilder;
    using Pipeline;
    using Pipeline.Contexts;
    using Transport;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class InvokeHandlersBehavior : IBehavior<IncomingContext>
    {
        public IDictionary<Type, Type> MessageDispatcherMappings { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            DispatchMessageToHandlersBasedOnType(context.Builder, context.IncomingLogicalMessage, context.MessageHandler);

            next();
        }

        void DispatchMessageToHandlersBasedOnType(IBuilder builder, LogicalMessage toHandle, MessageHandler messageHandler)
        {
            var handlerInstance = messageHandler.Instance;
            var handlerTypeToInvoke = handlerInstance.GetType();

            //for backwards compatibility (users can have registered their own factory
            var factory = GetDispatcherFactoryFor(handlerTypeToInvoke, builder);

            if (factory != null)
            {
                var dispatchers = factory.GetDispatcher(handlerTypeToInvoke, builder, toHandle.Instance).ToList();

                dispatchers.ForEach(dispatch =>
                {
                    log.DebugFormat("Dispatching message '{0}' to handler '{1}'", toHandle.MessageType, handlerTypeToInvoke);
                    try
                    {
                        dispatch();
                    }
                    catch (Exception e)
                    {
                        log.Warn(handlerTypeToInvoke.Name + " failed handling message.", e);

                        throw new TransportMessageHandlingFailedException(e);
                    }
                });
            }
            else
            {
                messageHandler.Invocation(handlerInstance, toHandle.Instance);
            }
        }

        IMessageDispatcherFactory GetDispatcherFactoryFor(Type messageHandlerTypeToInvoke, IBuilder builder)
        {
            if (MessageDispatcherMappings == null)
            {
                return null;
            }


            Type factoryType;

            MessageDispatcherMappings.TryGetValue(messageHandlerTypeToInvoke, out factoryType);

            if (factoryType == null)
            {
                return null;
            }

            var factory = builder.Build(factoryType) as IMessageDispatcherFactory;

            if (factory == null)
            {
                throw new InvalidOperationException(string.Format("Registered dispatcher factory {0} for type {1} does not implement IMessageDispatcherFactory", factoryType, messageHandlerTypeToInvoke));
            }

            return factory;
        }

        static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    }
}