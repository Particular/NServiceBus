using System;
using System.Collections.Generic;
using Rhino.Mocks;

namespace NServiceBus.Testing
{
    /// <summary>
    /// Message handler unit testing framework.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="K"></typeparam>
    public class Handler<T, K>  where K : IMessage, new() where T : IMessageHandler<K>, new()
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
        public Handler<T, K> WithExternalDependencies(Action<T> actionToSetUpExternalDependencies)
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
        public Handler<T, K> SetIncomingHeader(string key, string value)
        {
            incomingHeaders[key] = value;

            return this;
        }

        public Handler<T, K> AssertOutgoingHeader(string key, string value)
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
        public Handler<T, K> ExpectSend<TMessage>(SendPredicate<TMessage> check) where TMessage : IMessage
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
        public Handler<T, K> ExpectReply<TMessage>(SendPredicate<TMessage> check) where TMessage : IMessage
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
        public Handler<T, K> ExpectSendLocal<TMessage>(SendPredicate<TMessage> check) where TMessage : IMessage
        {
            helper.ExpectSendLocal(check);
            return this;
        }

        /// <summary>
        /// Check that the saga uses the bus to return the appropriate error code.
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        public Handler<T, K> ExpectReturn(ReturnPredicate check)
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
        public Handler<T, K> ExpectSendToDestination<TMessage>(SendToDestinationPredicate<TMessage> check) where TMessage : IMessage
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
        public Handler<T, K> ExpectPublish<TMessage>(PublishPredicate<TMessage> check) where TMessage : IMessage
        {
            helper.ExpectPublish(check);
            return this;
        }

        /// <summary>
        /// Activates the test that has been set up passing in the given message.
        /// </summary>
        /// <param name="initializeMessage"></param>
        public void OnMessage(Action<K> initializeMessage)
        {
            OnMessage(initializeMessage, Guid.NewGuid().ToString("N"));
        }

        /// <summary>
        /// Activates the test that has been set up passing in the given message, 
        /// setting the incoming headers and the message Id.
        /// </summary>
        /// <param name="initializeMessage"></param>
        /// <param name="messageId"></param>
        public void OnMessage(Action<K> initializeMessage, string messageId)
        {
            var context = new MessageContext { Id = messageId, ReturnAddress = "client", Headers = incomingHeaders };

            var msg = messageCreator.CreateInstance(initializeMessage);
            ExtensionMethods.CurrentMessageBeingHandled = msg;

            helper.Go(context, () => handler.Handle(msg));
            assertions.ForEach(a => a());

            assertions.Clear();
            ExtensionMethods.CurrentMessageBeingHandled = null;
        }
    }
}
