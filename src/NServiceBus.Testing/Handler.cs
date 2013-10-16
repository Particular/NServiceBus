namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Message handler unit testing framework.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Handler<T>
    {
        private readonly StubBus bus;
        private readonly T handler;
        private IDictionary<string, string> incomingHeaders = new Dictionary<string, string>();
        private readonly List<Action> assertions = new List<Action>();
        private IList<IExpectedInvocation> expectedInvocations = new List<IExpectedInvocation>();

        /// <summary>
        /// Creates a new instance of the handler tester.
        /// </summary>
        public Handler(T handler, StubBus bus, IMessageCreator messageCreator, IEnumerable<Type> types)
        {
            this.handler = handler;
            this.bus = bus;
        }

        /// <summary>
        /// Provides a way to set external dependencies on the handler under test.
        /// </summary>
        /// <param name="actionToSetUpExternalDependencies"></param>
        /// <returns></returns>
        public Handler<T> WithExternalDependencies(Action<T> actionToSetUpExternalDependencies)
        {
            actionToSetUpExternalDependencies(handler);

            return this;
        }

        /// <summary>
        /// Set the headers on an incoming message that will be return
        /// when code calls Bus.CurrentMessageContext.Headers
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Handler<T> SetIncomingHeader(string key, string value)
        {
            incomingHeaders[key] = value;

            return this;
        }

        /// <summary>
        /// Obsolete
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0")]
        public Handler<T> AssertOutgoingHeader(string key, string value)
        {
            return this;
        }

        /// <summary>
        /// Check that the handler sends a message of the given type complying with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Handler<T> ExpectSend<TMessage>(Func<TMessage, bool> check)
        {
            expectedInvocations.Add(new ExpectedSendInvocation<TMessage> { Check = check });
            return this;
        }

        /// <summary>
        /// Check that the handler does not send a message of the given type complying with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Handler<T> ExpectNotSend<TMessage>(Func<TMessage, bool> check)
        {
            expectedInvocations.Add(new ExpectedNotSendInvocation<TMessage> { Check = check });
            return this;
        }

        /// <summary>
        /// Check that the handler does not reply with a given message
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Handler<T> ExpectNotReply<TMessage>(Func<TMessage, bool> check)
        {
            expectedInvocations.Add(new ExpectedNotReplyInvocation<TMessage> { Check = check });
            return this;
        }

        /// <summary>
        /// Check that the handler replies with the given message type complying with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Handler<T> ExpectReply<TMessage>(Func<TMessage, bool> check)
        {
            expectedInvocations.Add(new ExpectedReplyInvocation<TMessage> { Check = check });
            return this;
        }

        /// <summary>
        /// Check that the handler sends the given message type to its local queue
        /// and that the message complies with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Handler<T> ExpectSendLocal<TMessage>(Func<TMessage, bool> check)
        {
            expectedInvocations.Add(new ExpectedSendLocalInvocation<TMessage> { Check = check });
            return this;
        }

        /// <summary>
        /// Check that the handler does not send a message type to its local queue that complies with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Handler<T> ExpectNotSendLocal<TMessage>(Func<TMessage, bool> check)
        {
            expectedInvocations.Add(new ExpectedNotSendLocalInvocation<TMessage> { Check = check });
            return this;
        }

        /// <summary>
        /// Check that the handler uses the bus to return the appropriate error code.
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        public Handler<T> ExpectReturn<TEnum>(Func<TEnum, bool> check)
        {
            expectedInvocations.Add(new ExpectedReturnInvocation<TEnum> { Check = check });
            return this;
        }

        /// <summary>
        /// Check that the handler sends the given message type to the appropriate destination.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Handler<T> ExpectSendToDestination<TMessage>(Func<TMessage, Address, bool> check)
        {
            expectedInvocations.Add(new ExpectedSendToDestinationInvocation<TMessage> { Check = check });
            return this;
        }

        /// <summary>
        /// Check that the handler publishes a message of the given type complying with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Handler<T> ExpectPublish<TMessage>(Func<TMessage, bool> check)
        {
            expectedInvocations.Add(new ExpectedPublishInvocation<TMessage> { Check = check });
            return this;
        }

        /// <summary>
        /// Check that the handler does not publish any messages of the given type complying with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Handler<T> ExpectNotPublish<TMessage>(Func<TMessage, bool> check)
        {
            expectedInvocations.Add(new ExpectedNotPublishInvocation<TMessage> { Check = check });
            return this;
        }

        /// <summary>
        /// Check that the handler tells the bus to stop processing the current message.
        /// </summary>
        /// <returns></returns>
        public Handler<T> ExpectDoNotContinueDispatchingCurrentMessageToHandlers()
        {
            expectedInvocations.Add(new ExpectedDoNotContinueDispatchingCurrentMessageToHandlersInvocation<object>());
            return this;
        }

        /// <summary>
        /// Check that the handler tells the bus to forward the current message to the given destination.
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        [ObsoleteEx(Message = "Please use an integration test to validate this feature.", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public Handler<T> ExpectForwardCurrentMessageTo(string destination)
        {
            expectedInvocations.Add(new ExpectedForwardCurrentMessageToInvocation { Check = d => d == destination });
            return this;
        }

        /// <summary>
        /// Check that the handler tells the bus to handle the current message later.
        /// </summary>
        /// <returns></returns>
        public Handler<T> ExpectHandleCurrentMessageLater()
        {
            expectedInvocations.Add(new ExpectedHandleCurrentMessageLaterInvocation<object>());
            return this;
        }

        /// <summary>
        /// Check that the handler sends a message of the given type to sites
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Handler<T> ExpectSendToSites<TMessage>(Func<TMessage, IEnumerable<string>, bool> check)
        {
            expectedInvocations.Add(new ExpectedSendToSitesInvocation<TMessage> { Check = check });
            return this;
        }

        /// <summary>
        /// Check that the handler doesn't send a message of the given type to sites
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Handler<T> ExpectNotSendToSites<TMessage>(Func<TMessage, IEnumerable<string>, bool> check)
        {
            expectedInvocations.Add(new ExpectedNotSendToSitesInvocation<TMessage> { Check = check });
            return this;
        }

        /// <summary>
        /// Activates the test that has been set up passing in the given message.
        /// </summary>
        /// <param name="initializeMessage"></param>
        public void OnMessage<TMessage>(Action<TMessage> initializeMessage = null)
        {
            OnMessage(Guid.NewGuid().ToString("N"), initializeMessage);
        }

        /// <summary>
        /// Activates the test that has been set up passing in the given message, 
        /// setting the incoming headers and the message Id.
        /// </summary>
        /// <param name="initializeMessage"></param>
        /// <param name="messageId"></param>
        public void OnMessage<TMessage>(string messageId, Action<TMessage> initializeMessage = null)
        {
            var msg = bus.CreateInstance(initializeMessage);
            OnMessage(msg, messageId);
        }

        /// <summary>Activates the test that has been set up passing in a specific message to be used.</summary>
        /// <param name="initializedMessage">A message to be used with message handler.</param>
        /// <remarks>This is different from "<![CDATA[public void OnMessage<TMessage>(Action<TMessage> initializedMessage)]]>" in a way that it uses the message, and not calls to an action.</remarks>
        /// <example><![CDATA[var message = new TestMessage {//...}; Test.Handler<EmptyHandler>().OnMessage<TestMessage>(message);]]></example>
        public void OnMessage<TMessage>(TMessage initializedMessage)
        {
            OnMessage(initializedMessage, Guid.NewGuid().ToString("N"));
        }

        /// <summary>
        /// Activates the test that has been set up passing in given message,
        /// setting the incoming headers and the message Id.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="message"></param>
        /// <param name="messageId"></param>
        public void OnMessage<TMessage>(TMessage message, string messageId)
        {
            var context = new MessageContext { Id = messageId, ReturnAddress = "client", Headers = incomingHeaders };
            bus.CurrentMessageContext = context;

            foreach (var keyValuePair in incomingHeaders)
                ExtensionMethods.SetHeaderAction(message, keyValuePair.Key, keyValuePair.Value);

            ExtensionMethods.CurrentMessageBeingHandled = message;

            try
            {
                var method = GetMessageHandler(handler.GetType(), typeof(TMessage));
                method.Invoke(handler, new object[] {message});
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }

            bus.ValidateAndReset(expectedInvocations);
            expectedInvocations.Clear();

            assertions.ForEach(a => a());

            assertions.Clear();
            ExtensionMethods.CurrentMessageBeingHandled = null;
        }

        private static MethodInfo GetMessageHandler(Type targetType, Type messageType)
        {
            var method = targetType.GetMethod("Handle", new[] { messageType });
            if (method != null) return method;

            var handlerType = typeof(IHandleMessages<>).MakeGenericType(messageType);
            return targetType.GetInterfaceMap(handlerType)
                            .TargetMethods
                            .FirstOrDefault();
        }
    }
}
