using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rhino.Mocks;

namespace NServiceBus.Testing
{
    /// <summary>
    /// Message handler unit testing framework.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Handler<T>
    {
        private readonly Helper helper;
        private readonly T handler;
        private readonly IMessageCreator messageCreator;
        private IDictionary<string, string> incomingHeaders = new Dictionary<string, string>();
        private readonly IDictionary<string, string> outgoingHeaders = new Dictionary<string, string>();
        private readonly List<Action> assertions = new List<Action>();

        /// <summary>
        /// Creates a new instance of the handler tester.
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="mocks"></param>
        /// <param name="bus"></param>
        /// <param name="messageCreator"></param>
        /// <param name="types"></param>
        public Handler(T handler, MockRepository mocks, IBus bus, IMessageCreator messageCreator, IEnumerable<Type> types)
        {
            this.handler = handler;
            this.messageCreator = messageCreator;
            helper = new Helper(mocks, bus, messageCreator, types);

            var headers = bus.OutgoingHeaders;
            LastCall.Repeat.Any().Return(outgoingHeaders);
        }

        /// <summary>
        /// Provides a way to set external dependencies on the saga under test.
        /// </summary>
        /// <param name="actionToSetUpExternalDependencies"></param>
        /// <returns></returns>
        public Handler<T> WithExternalDependencies(Action<T> actionToSetUpExternalDependencies)
        {
            actionToSetUpExternalDependencies(handler);

            return this;
        }

        /// <summary>
        /// Get the headers set by the saga when it sends a message.
        /// </summary>
        public IDictionary<string, string> OutgoingHeaders
        {
            get { return outgoingHeaders; }
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
        /// Asserts that the given value is stored under the given key in the outgoing headers.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Handler<T> AssertOutgoingHeader(string key, string value)
        {
            assertions.Add(() =>
                               {
                                   string v;
                                   outgoingHeaders.TryGetValue(key, out v);

                                   if (v != value)
                                       throw new Exception("Outgoing header value for key '" + key + "' was '" + v +
                                                           "' instead of '" + value + "'.");
                               });

            return this;
        }

        /// <summary>
        /// Check that the saga sends a message of the given type complying with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Handler<T> ExpectSend<TMessage>(SendPredicate<TMessage> check) where TMessage : IMessage
        {
            helper.ExpectSend(check);
            return this;
        }

        /// <summary>
        /// Check that the saga replies with the given message type complying with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Handler<T> ExpectReply<TMessage>(SendPredicate<TMessage> check) where TMessage : IMessage
        {
            helper.ExpectReply(check);
            return this;
        }

        /// <summary>
        /// Check that the saga sends the given message type to its local queue
        /// and that the message complies with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Handler<T> ExpectSendLocal<TMessage>(SendPredicate<TMessage> check) where TMessage : IMessage
        {
            helper.ExpectSendLocal(check);
            return this;
        }

        /// <summary>
        /// Check that the saga uses the bus to return the appropriate error code.
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        public Handler<T> ExpectReturn(ReturnPredicate check)
        {
            helper.ExpectReturn(check);
            return this;
        }

        /// <summary>
        /// Check that the saga sends the given message type to the appropriate destination.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Handler<T> ExpectSendToDestination<TMessage>(SendToDestinationPredicate<TMessage> check) where TMessage : IMessage
        {
            helper.ExpectSendToDestination(check);
            return this;
        }

        /// <summary>
        /// Check that the saga publishes a message of the given type complying with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Handler<T> ExpectPublish<TMessage>(PublishPredicate<TMessage> check) where TMessage : IMessage
        {
            helper.ExpectPublish(check);
            return this;
        }

        /// <summary>
        /// Check that the saga does not publish any messages of the given type complying with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Handler<T> ExpectNotPublish<TMessage>(PublishPredicate<TMessage> check) where TMessage : IMessage
        {
          helper.ExpectNotPublish(check);
          return this;
        }

        /// <summary>
        /// Check that the handler tells the bus to stop processing the current message.
        /// </summary>
        /// <returns></returns>
        public Handler<T> ExpectDoNotContinueDispatchingCurrentMessageToHandlers()
        {
            helper.ExpectDoNotContinueDispatchingCurrentMessageToHandlers();
            return this;
        }

        /// <summary>
        /// Check that the handler tells the bus to handle the current message later.
        /// </summary>
        /// <returns></returns>
        public Handler<T> ExpectHandleCurrentMessageLater()
        {
            helper.ExpectHandleCurrentMessageLater();
            return this;
        }

        /// <summary>
        /// Activates the test that has been set up passing in the given message.
        /// </summary>
        /// <param name="initializeMessage"></param>
        public void OnMessage<TMessage>(Action<TMessage> initializeMessage) where TMessage : IMessage
        {
            OnMessage(initializeMessage, Guid.NewGuid().ToString("N"));
        }

        /// <summary>
        /// Activates the test that has been set up passing in the given message, 
        /// setting the incoming headers and the message Id.
        /// </summary>
        /// <param name="initializeMessage"></param>
        /// <param name="messageId"></param>
        public void OnMessage<TMessage>(Action<TMessage> initializeMessage, string messageId) where TMessage : IMessage
        {
            var context = new MessageContext { Id = messageId, ReturnAddress = "client", Headers = incomingHeaders };

            var msg = messageCreator.CreateInstance(initializeMessage);
            ExtensionMethods.CurrentMessageBeingHandled = msg;

            MethodInfo method = GetMessageHandler(handler.GetType(), typeof(TMessage));
            helper.Go(context, () => method.Invoke(handler, new object[] { msg }));

            assertions.ForEach(a => a());

            assertions.Clear();
            ExtensionMethods.CurrentMessageBeingHandled = null;
        }

        private static MethodInfo GetMessageHandler(Type targetType, Type messageType) 
        {
			var method = targetType.GetMethod("Handle", new[] { messageType });
			if (method != null) return method;

			var handlerType = typeof(IMessageHandler<>).MakeGenericType(messageType);
			return targetType.GetInterfaceMap(handlerType)
			                .TargetMethods
			                .FirstOrDefault();
		}
    }
}
