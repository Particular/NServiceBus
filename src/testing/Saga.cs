using System;
using System.Collections.Generic;
using NServiceBus.Saga;
using Rhino.Mocks;

namespace NServiceBus.Testing
{
    /// <summary>
    /// Saga unit testing framework.
    /// </summary>
    public class Saga<T> where T : ISaga, new()
    {
        private readonly Helper helper;
        private readonly T saga;
        private string messageId;
        private string clientAddress;
        private IDictionary<string, string> incomingHeaders = new Dictionary<string, string>();
        private IDictionary<string, string> outgoingHeaders = new Dictionary<string, string>();

        internal Saga(T saga, MockRepository mocks, IBus bus, IMessageCreator messageCreator, List<Type> types)
        {
            this.saga = saga;
            helper = new Helper(mocks, bus, messageCreator, types);

            var headers = bus.OutgoingHeaders;
            LastCall.Repeat.Any().Return(outgoingHeaders);
        }

        /// <summary>
        /// Provides a way to set external dependencies on the saga under test.
        /// </summary>
        /// <param name="actionToSetUpExternalDependencies"></param>
        /// <returns></returns>
        public Saga<T> WithExternalDependencies(Action<T> actionToSetUpExternalDependencies)
        {
            actionToSetUpExternalDependencies(saga);

            return this;
        }

        /// <summary>
        /// Set the address of the client that caused the saga to be started.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public Saga<T> WhenReceivesMessageFrom(string client)
        {
            clientAddress = client;
            saga.Entity.Originator = client;

            return this;
        }

        /// <summary>
        /// Set the headers on an incoming message that will be return
        /// when code calls Bus.CurrentMessageContext.Headers
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Saga<T> SetIncomingHeader(string key, string value)
        {
            incomingHeaders[key] = value;

            return this;
        }

        /// <summary>
        /// Sets the Id of the incoming message that will be returned
        /// when code calls Bus.CurrentMessageContext.Id
        /// </summary>
        /// <param name="messageId"></param>
        /// <returns></returns>
        public Saga<T> SetMessageId(string messageId)
        {
            this.messageId = messageId;

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
        /// Check that the saga sends a message of the given type complying with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Saga<T> ExpectSend<TMessage>(SendPredicate<TMessage> check) where TMessage : IMessage
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
        public Saga<T> ExpectReply<TMessage>(SendPredicate<TMessage> check) where TMessage : IMessage
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
        public Saga<T> ExpectSendLocal<TMessage>(SendPredicate<TMessage> check) where TMessage : IMessage
        {
            helper.ExpectSendLocal(check);
            return this;
        }

        /// <summary>
        /// Check that the saga uses the bus to return the appropriate error code.
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        public Saga<T> ExpectReturn(ReturnPredicate check)
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
        public Saga<T> ExpectSendToDestination<TMessage>(SendToDestinationPredicate<TMessage> check) where TMessage : IMessage
        {
            helper.ExpectSendToDestination(check);
            return this;
        }

        /// <summary>
        /// Check that the saga replies to the originator with the given message type.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Saga<T> ExpectReplyToOrginator<TMessage>(SendPredicate<TMessage> check) where TMessage : IMessage
        {
            helper.ExpectReplyToOrginator(check);
            return this;
        }

        /// <summary>
        /// Check that the saga publishes a message of the given type complying with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Saga<T> ExpectPublish<TMessage>(PublishPredicate<TMessage> check) where TMessage : IMessage
        {
            helper.ExpectPublish(check);
            return this;
        }

        /// <summary>
        /// Uses the given delegate to invoke the saga, checking all the expectations previously set up,
        /// and then clearing them for continued testing.
        /// </summary>
        /// <param name="sagaIsInvoked"></param>
        public Saga<T> When(Action<T> sagaIsInvoked)
        {
            var context = new MessageContext { Id = messageId, ReturnAddress = clientAddress, Headers = incomingHeaders };

            helper.Go(context, () => sagaIsInvoked(saga));

            return this;
        }

        /// <summary>
        /// Asserts that the saga is either complete or not.
        /// </summary>
        /// <param name="complete"></param>
        /// <returns></returns>
        public Saga<T> AssertSagaCompletionIs(bool complete)
        {
            if (saga.Completed == complete)
                return this;

            if (saga.Completed)
                throw new Exception("Assert failed. Saga has been completed.");
            
            throw new Exception("Assert failed. Saga has not been completed.");
        }

    }

}
