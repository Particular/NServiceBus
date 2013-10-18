namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Logging;
    using ObjectBuilder;
    using Saga;
    using Sagas;
    using Unicast;
    using Unicast.Transport;

    /// <summary>
    /// invoke handler'n'stuff
    /// </summary>
    class InvokeHandlersBehavior : IBehavior
    {
        public IBehavior Next { get; set; }

        public IBuilder Builder { get; set; }

  
        public void Invoke(BehaviorContext context)
        {
            var messages = context.Messages;

            if (context.Messages == null)
            {
                var error = string.Format("Messages has not been set on the current behavior context: {0} - DispatchToHandlers must be executed AFTER having extracted the messages", context);
                throw new ArgumentException(error);
            }

            var messageHandlers = context.Get<LoadedMessageHandlers>();

            foreach (var messageToHandle in messages)
            {
                ExtensionMethods.CurrentMessageBeingHandled = messageToHandle;

                var handlersToInvoke = messageHandlers.GetHandlersFor(messageToHandle.GetType());

                DispatchMessageToHandlersBasedOnType(Builder, messageToHandle, handlersToInvoke);
            }

            ExtensionMethods.CurrentMessageBeingHandled = null;

            Next.Invoke(context);
        }

        /// <summary>
        /// Finds the message handlers associated with the message type and dispatches
        /// the message to the found handlers.
        /// </summary>
        /// <param name="builder">The builder used to construct the handlers.</param>
        /// <param name="toHandle">The message to dispatch to the handlers.</param>
        /// <remarks>
        /// If during the dispatch, a message handler calls the DoNotContinueDispatchingCurrentMessageToHandlers method,
        /// this prevents the message from being further dispatched.
        /// This includes generic message handlers (of IMessage), and handlers for the specific messageType.
        /// </remarks>
        void DispatchMessageToHandlersBasedOnType(IBuilder builder, object toHandle,IEnumerable<object> handlers)
        {
            var messageType = toHandle.GetType();


            foreach (var handler in handlers)
            {
                try
                {
                    //until we have a outgoing pipeline that inherits context from the main one
                    if (handler is ISaga)
                    {
                        SagaContext.Current = (ISaga) handler;
                    }
                    var handlerTypeToInvoke = handler.GetType();

                    //for backwards compatibility (users can have registered their own factory
                    var factory = GetDispatcherFactoryFor(handlerTypeToInvoke, builder);

                    if (factory != null)
                    {
                        var dispatchers = factory.GetDispatcher(handlerTypeToInvoke, builder, toHandle).ToList();

                        dispatchers.ForEach(dispatch =>
                        {
                            log.DebugFormat("Dispatching message '{0}' to handler '{1}'", messageType, handlerTypeToInvoke);
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
                        HandlerInvocationCache.InvokeHandle(handler, toHandle);
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
            Builder.Build<UnicastBus>(). MessageDispatcherMappings.TryGetValue(messageHandlerTypeToInvoke, out factoryType);

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