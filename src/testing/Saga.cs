using System;
using System.Collections.Generic;
using System.Diagnostics;
using NServiceBus.Saga;

namespace NServiceBus.Testing
{
    /// <summary>
    /// Saga unit testing framework.
    /// </summary>
    public class Saga<T> where T : ISaga, new()
    {
        private readonly T saga;
        private readonly StubBus bus;
        private Func<string> messageId = () => Guid.NewGuid().ToString();
        private IDictionary<string, string> incomingHeaders = new Dictionary<string, string>();
        private IList<IExpectedInvocation> expectedInvocations = new List<IExpectedInvocation>();

        internal Saga(T saga, StubBus bus)
        {
            this.saga = saga;
            this.bus = bus;

            saga.Entity.OriginalMessageId = Guid.NewGuid().ToString();
            saga.Entity.Originator = "client";
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
            this.messageId = () => messageId;

            return this;
        }

        /// <summary>
        /// Get the headers set by the saga when it sends a message.
        /// </summary>
        public IDictionary<string, string> OutgoingHeaders
        {
            get { return bus.OutgoingHeaders; }
        }

        /// <summary>
        /// Check that the saga sends a message of the given type complying with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Saga<T> ExpectSend<TMessage>(Func<TMessage,bool> check = null)
        {
            expectedInvocations.Add(new ExpectedSendInvocation<TMessage> { Check = check});
            return this;
        }

        /// <summary>
        /// Check that the saga does not send a message of the given type complying with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Saga<T> ExpectNotSend<TMessage>(Func<TMessage, bool> check)
        {
            expectedInvocations.Add(new ExpectedNotSendInvocation<TMessage> { Check = check });
            return this;
        }

        /// <summary>
        /// Check that the saga replies with the given message type complying with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        [Obsolete("Sagas should never call Reply, instead they should call ReplyToOriginator which should be tested with ExpectReplyToOriginator.")]
        public Saga<T> ExpectReply<TMessage>(Func<TMessage, bool> check = null)
        {
            throw new InvalidOperationException("Sagas should never call Reply, instead they should call ReplyToOriginator which should be tested with ExpectReplyToOriginator.");
        }

        /// <summary>
        /// Check that the saga sends the given message type to its local queue
        /// and that the message complies with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Saga<T> ExpectSendLocal<TMessage>(Func<TMessage, bool> check = null)
        {
            expectedInvocations.Add(new ExpectedSendLocalInvocation<TMessage> { Check = check });
            return this;
        }

        /// <summary>
        /// Check that the saga does not send a message type to its local queue that complies with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Saga<T> ExpectNotSendLocal<TMessage>(Func<TMessage, bool> check)
        {
            expectedInvocations.Add(new ExpectedNotSendLocalInvocation<TMessage> { Check = check });
            return this;
        }

        /// <summary>
        /// Check that the saga uses the bus to return the appropriate error code.
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        [Obsolete("Sagas should never call Return, instead they should call ReplyToOriginator which should be tested with ExpectReplyToOriginator.")]
        public Saga<T> ExpectReturn(Func<int, bool> check = null)
        {
            throw new InvalidOperationException("Sagas should never call Return, instead they should call ReplyToOriginator which should be tested with ExpectReplyToOriginator.");
        }

        /// <summary>
        /// Check that the saga sends the given message type to the appropriate destination.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Saga<T> ExpectSendToDestination<TMessage>(Func<TMessage, Address, bool> check)
        {
            expectedInvocations.Add(new ExpectedSendToDestinationInvocation<TMessage> { Check = check });

            return this;
        }

        /// <summary>
        /// Check that the saga replies to the originator with the given message type.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Saga<T> ExpectReplyToOrginator<TMessage>(Func<TMessage, bool> check = null)
        {
            expectedInvocations.Add(new ExpectedReplyToOriginatorInvocation<TMessage>
                                        {
                                            Check = (msg, address, correlationId) =>
                                                        {
                                                            if (address != Address.Parse(saga.Entity.Originator))
                                                            {
                                                                throw new Exception(
                                                                    "Expected ReplyToOriginator. Messages were sent to " +
                                                                    address + " instead.");
                                                                return false;
                                                            }

                                                            if (correlationId != saga.Entity.OriginalMessageId)
                                                            {
                                                                throw new Exception(
                                                                    "Expected ReplyToOriginator. Messages were sent with correlation ID " +
                                                                    correlationId + " instead of " + saga.Entity.OriginalMessageId);
                                                                return false;
                                                            }

                                                            if (check != null)
                                                                return check(msg);

                                                            return true;
                                                        }
                                        }
                );
            return this;
        }

        /// <summary>
        /// Check that the saga publishes a message of the given type complying with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Saga<T> ExpectPublish<TMessage>(Func<TMessage, bool> check = null)
        {
            expectedInvocations.Add(new ExpectedPublishInvocation<TMessage> { Check = check });
            return this;
        }
      
        /// <summary>
        /// Check that the saga does not publish any messages of the given type complying with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Saga<T> ExpectNotPublish<TMessage>(Func<TMessage, bool> check = null)
        {
            expectedInvocations.Add(new ExpectedNotPublishInvocation<TMessage> { Check = check });
            return this;
        }

        /// <summary>
        /// Check that the saga tells the bus to handle the current message later.
        /// </summary>
        /// <returns></returns>
        public Saga<T> ExpectHandleCurrentMessageLater()
        {
            expectedInvocations.Add(new ExpectedHandleCurrentMessageLaterInvocation<object>());
            return this;
        }

        /// <summary>
        /// Uses the given delegate to invoke the saga, checking all the expectations previously set up,
        /// and then clearing them for continued testing.
        /// </summary>
        /// <param name="sagaIsInvoked"></param>
        public Saga<T> When(Action<T> sagaIsInvoked)
        {
            var id = messageId();

            var context = new MessageContext { Id = id, ReturnAddress = saga.Entity.Originator, Headers = incomingHeaders };
            bus.CurrentMessageContext = context;

            sagaIsInvoked(saga);

            bus.ValidateAndReset(expectedInvocations);
            expectedInvocations.Clear();

            Trace.WriteLine("Finished when invocation.");

            messageId = () => Guid.NewGuid().ToString();
            return this;
        }

        /// <summary>
        /// Invokes the saga timeout passing in the last timeout state it sent
        /// and then clears out all previous expectations.
        /// </summary>
        /// <returns></returns>
        public Saga<T> WhenSagaTimesOut()
        {
            var state = bus.PopTimeout();
            var method = saga.GetType().GetMethod("Timeout", new[] {state.GetType()});
            
            return When(s => method.Invoke(s, new[] {state}));
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

        /// <summary>
        /// Verifies that the saga is setting the specified timeout
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Saga<T> ExpectTimeoutToBeSetIn<TMessage>(Func<TMessage, TimeSpan, bool> check = null)
        {
            expectedInvocations.Add(new ExpectedDeferMessageInvocation<TMessage, TimeSpan> { Check = check });
            return this;
        }

        /// <summary>
        /// Verifies that the saga is setting the specified timeout
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Saga<T> ExpectTimeoutToBeSetAt<TMessage>(Func<TMessage, DateTime, bool> check = null)
        {
            expectedInvocations.Add(new ExpectedDeferMessageInvocation<TMessage, DateTime> { Check = check });
            return this;
        }
    }

}
