namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Logging;
    using ObjectBuilder;
    using Unicast;
    using Unicast.Transport;

    /// <summary>
    /// invoke handler'n'stuff
    /// </summary>
    class InvokeHandlersBehavior : IBehavior
    {
        public IBehavior Next { get; set; }

        public IBuilder Builder { get; set; }

        public IMessageHandlerRegistry HandlerRegistry { get; set; }

        public void Invoke(BehaviorContext context)
        {
            var messages = context.Messages;

            // for now we cheat and pull it from the behavior context:
            var callbackInvoked = BehaviorContext.Current.Get<bool>(CallbackInvocationBehavior.CallbackInvokedKey);

            foreach (var messageToHandle in messages)
            {
                ExtensionMethods.CurrentMessageBeingHandled = messageToHandle;

                var handlers = DispatchMessageToHandlersBasedOnType(Builder, messageToHandle).ToList();

                if (!callbackInvoked && !handlers.Any())
                {
                    var error = string.Format("No handlers could be found for message type: {0}", messageToHandle.GetType().FullName);
                    throw new InvalidOperationException(error);
                }
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
        IEnumerable<Type> DispatchMessageToHandlersBasedOnType(IBuilder builder, object toHandle)
        {
            var invokedHandlers = new List<Type>();
            var messageType = toHandle.GetType();

            foreach (var handlerType in HandlerRegistry.GetHandlerTypes(messageType))
            {
                var handlerTypeToInvoke = handlerType;

                var factory = GetDispatcherFactoryFor(handlerTypeToInvoke, builder);

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
                        log.Warn(handlerType.Name + " failed handling message.", e);

                        throw new TransportMessageHandlingFailedException(e);
                    }
                });

                invokedHandlers.Add(handlerTypeToInvoke);
            }

            return invokedHandlers;
        }

        IMessageDispatcherFactory GetDispatcherFactoryFor(Type messageHandlerTypeToInvoke, IBuilder builder)
        {
            Type factoryType;

            //todo: Move the dispatcher mappings here (also obsolete the feature)
            Builder.Build<UnicastBus>(). MessageDispatcherMappings.TryGetValue(messageHandlerTypeToInvoke, out factoryType);

            if (factoryType == null)
                throw new InvalidOperationException("No dispatcher factory type configured for messageHandler " + messageHandlerTypeToInvoke);

            var factory = builder.Build(factoryType) as IMessageDispatcherFactory;

            if (factory == null)
                throw new InvalidOperationException(string.Format("Registered dispatcher factory {0} for type {1} does not implement IMessageDispatcherFactory", factoryType, messageHandlerTypeToInvoke));

            return factory;
        }

        static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
   
    }
}