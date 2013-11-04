namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Logging;
    using ObjectBuilder;
    using Saga;
    using Sagas;
    using Unicast;
    using Unicast.Transport;

    class InvokeHandlersBehavior : IBehavior<PhysicalMessageContext>
    {
        public void Invoke(PhysicalMessageContext context, Action next)
        {
            var messageHandlers = context.Get<LoadedMessageHandlers>();

            foreach (var messageToHandle in context.Get<LogicalMessages>())
            {
                ExtensionMethods.CurrentMessageBeingHandled = messageToHandle.Instance;

                DispatchMessageToHandlersBasedOnType(context.Builder, messageToHandle, messageHandlers,context);
            }

            ExtensionMethods.CurrentMessageBeingHandled = null;

            next();
        }

        void DispatchMessageToHandlersBasedOnType(IBuilder builder, LogicalMessage toHandle, LoadedMessageHandlers loadedHandlers, BehaviorContext context)
        {
            foreach (var loadedHandler in loadedHandlers.GetHandlersFor(toHandle.MessageType))
            {
                if (loadedHandler.InvocationDisabled)
                    continue;

                var handlerInstance = loadedHandler.Instance;
                try
                {
                    //until we have a outgoing pipeline that inherits context from the main one
                    if (handlerInstance is ISaga)
                    {
                        SagaContext.Current = (ISaga) handlerInstance;
                    }
                    
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
                        loadedHandler.Invocation(handlerInstance, toHandle.Instance);
                    }

                    //for now we have to check of the chain is aborted but this will go away when we refactor the handlers to be a subpipeline
                    if (context.ChainAborted)
                    {
                        log.DebugFormat("Handler {0} requested downstream handlers of message {1} to not be invoked", handlerTypeToInvoke,toHandle.MessageType);
                        return;
                    }
                }
                finally
                {
                    SagaContext.Current = null;
                }
            }
        }

        IMessageDispatcherFactory GetDispatcherFactoryFor(Type messageHandlerTypeToInvoke, IBuilder builder)
        {
            Type factoryType;

            //todo: Move the dispatcher mappings here (also obsolete the feature)
            builder.Build<UnicastBus>().MessageDispatcherMappings.TryGetValue(messageHandlerTypeToInvoke, out factoryType);

            if (factoryType == null)
                return null;

            var factory = builder.Build(factoryType) as IMessageDispatcherFactory;

            if (factory == null)
                throw new InvalidOperationException(string.Format("Registered dispatcher factory {0} for type {1} does not implement IMessageDispatcherFactory", factoryType, messageHandlerTypeToInvoke));

            return factory;
        }

        static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
   
    }
}