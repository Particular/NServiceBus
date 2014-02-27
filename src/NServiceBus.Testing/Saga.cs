namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using Saga;

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
            if (saga.Entity == null)
            {
                var prop = typeof(T).GetProperty("Data");
                var sagaData = Activator.CreateInstance(prop.PropertyType) as IContainSagaData;
                saga.Entity = sagaData;
            }
            saga.Entity.OriginalMessageId = Guid.NewGuid().ToString();
            saga.Entity.Originator = "client";
        }
        /// <summary>
        /// Provides a way to set external dependencies on the saga under test.
        /// </summary>
        public Saga<T> WithExternalDependencies(Action<T> actionToSetUpExternalDependencies)
        {
            actionToSetUpExternalDependencies(saga);

            return this;
        }

        /// <summary>
        /// Set the address of the client that caused the saga to be started.
        /// </summary>
        public Saga<T> WhenReceivesMessageFrom(string client)
        {
            saga.Entity.Originator = client;

            return this;
        }

        /// <summary>
        /// Set the headers on an incoming message that will be return
        /// when code calls Bus.CurrentMessageContext.Headers
        /// </summary>
        public Saga<T> SetIncomingHeader(string key, string value)
        {
            incomingHeaders[key] = value;

            return this;
        }

        /// <summary>
        /// Sets the Id of the incoming message that will be returned
        /// when code calls Bus.CurrentMessageContext.Id
        /// </summary>
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
        public Saga<T> ExpectSend<TMessage>(Func<TMessage,bool> check = null)
        {
            expectedInvocations.Add(new ExpectedSendInvocation<TMessage> { Check = check});
            return this;
        }

        /// <summary>
        /// Check that the saga sends a message of the given type complying with user-supplied assertions.
        /// </summary>
        /// <param name="check">An action containing assertions on the message.</param>
        public Saga<T> ExpectSend<TMessage>(Action<TMessage> check)
        {
            return ExpectSend(CheckActionToFunc(check));
        }

        /// <summary>
        /// Check that the saga does not send a message of the given type complying with the given predicate.
        /// </summary>
        public Saga<T> ExpectNotSend<TMessage>(Func<TMessage, bool> check)
        {
            expectedInvocations.Add(new ExpectedNotSendInvocation<TMessage> { Check = check });
            return this;
        }

        /// <summary>
        /// Check that the saga does not send a message of the given type complying with the given predicate.
        /// </summary>
        /// <param name="check">An action containing assertions on the message.</param>
        public Saga<T> ExpectNotSend<TMessage>(Action<TMessage> check)
        {
            expectedInvocations.Add(new ExpectedNotSendInvocation<TMessage> { Check = CheckActionToFunc(check) });
            return this;
        }

        /// <summary>
        /// Check that the saga replies with the given message type complying with the given predicate.
        /// </summary>
        public Saga<T> ExpectReply<TMessage>(Func<TMessage, bool> check = null)
        {
            expectedInvocations.Add(new ExpectedReplyInvocation<TMessage> { Check = check });
            return this;
        }

        /// <summary>
        /// Check that the saga sends the given message type to its local queue
        /// and that the message complies with the given predicate.
        /// </summary>
        public Saga<T> ExpectSendLocal<TMessage>(Func<TMessage, bool> check = null)
        {
            expectedInvocations.Add(new ExpectedSendLocalInvocation<TMessage> { Check = check });
            return this;
        }

        /// <summary>
        /// Check that the saga sends the given message type to its local queue
        /// and that the message complies with the given predicate.
        /// </summary>
        /// <param name="check">An action that performs assertions on the message.</param>
        public Saga<T> ExpectSendLocal<TMessage>(Action<TMessage> check)
        {
            return ExpectSendLocal(CheckActionToFunc(check));
        }

        /// <summary>
        /// Check that the saga does not send a message type to its local queue that complies with the given predicate.
        /// </summary>
        public Saga<T> ExpectNotSendLocal<TMessage>(Func<TMessage, bool> check)
        {
            expectedInvocations.Add(new ExpectedNotSendLocalInvocation<TMessage> { Check = check });
            return this;
        }

        /// <summary>
        /// Check that the saga does not send a message type to its local queue that complies with the given predicate.
        /// </summary>
        /// <param name="check">An action that performs assertions on the message.</param>
        public Saga<T> ExpectNotSendLocal<TMessage>(Action<TMessage> check)
        {
            return ExpectNotSendLocal(CheckActionToFunc(check));
        }

        /// <summary>
        /// Check that the saga uses the bus to return the appropriate error code.
        /// </summary>
        [ObsoleteEx(Message = "Sagas should never call Return, instead they should call ReplyToOriginator which should be tested with ExpectReplyToOriginator.", RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0")]
        public Saga<T> ExpectReturn(Func<int, bool> check = null)
        {
            throw new InvalidOperationException("Sagas should never call Return, instead they should call ReplyToOriginator which should be tested with ExpectReplyToOriginator.");
        }

        /// <summary>
        /// Check that the saga sends the given message type to the appropriate destination.
        /// </summary>
        public Saga<T> ExpectSendToDestination<TMessage>(Func<TMessage, Address, bool> check)
        {
            expectedInvocations.Add(new ExpectedSendToDestinationInvocation<TMessage> { Check = check });

            return this;
        }

        /// <summary>
        /// Check that the saga sends the given message type to the appropriate destination.
        /// </summary>
        /// <param name="check">An action that performs assertions on the message.</param>
        public Saga<T> ExpectSendToDestination<TMessage>(Action<TMessage, Address> check)
        {
            return ExpectSendToDestination(CheckActionToFunc(check));
        }

        /// <summary>
        /// Check that the saga replies to the originator with the given message type.
        /// </summary>
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
                                                            }

                                                            if (correlationId != saga.Entity.OriginalMessageId)
                                                            {
                                                                throw new Exception(
                                                                    "Expected ReplyToOriginator. Messages were sent with correlation ID " +
                                                                    correlationId + " instead of " + saga.Entity.OriginalMessageId);
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
        /// Check that the saga replies to the originator with the given message type.
        /// </summary>
        /// <param name="check">An action that performs assertions on the message.</param>
        public Saga<T> ExpectReplyToOrginator<TMessage>(Action<TMessage> check)
        {
            return ExpectReplyToOrginator(CheckActionToFunc(check));
        }
        
        /// <summary>
        /// Check that the saga publishes a message of the given type complying with the given predicate.
        /// </summary>
        public Saga<T> ExpectPublish<TMessage>(Func<TMessage, bool> check = null)
        {
            expectedInvocations.Add(new ExpectedPublishInvocation<TMessage> { Check = check });
            return this;
        }

        /// <summary>
        /// Check that the saga publishes a message of the given type complying with the given predicate.
        /// </summary>
        /// <param name="check">An action that performs assertions on the message.</param>
        public Saga<T> ExpectPublish<TMessage>(Action<TMessage> check)
        {
            return ExpectPublish(CheckActionToFunc(check));
        }

        /// <summary>
        /// Check that the saga does not publish any messages of the given type complying with the given predicate.
        /// </summary>
        public Saga<T> ExpectNotPublish<TMessage>(Func<TMessage, bool> check = null)
        {
            expectedInvocations.Add(new ExpectedNotPublishInvocation<TMessage> { Check = check });
            return this;
        }

        /// <summary>
        /// Check that the saga does not publish any messages of the given type complying with the given predicate.
        /// </summary>
        /// <param name="check">An action that performs assertions on the message.</param>
        public Saga<T> ExpectNotPublish<TMessage>(Action<TMessage> check)
        {
            return ExpectNotPublish(CheckActionToFunc(check));
        }

        /// <summary>
        /// Check that the saga tells the bus to handle the current message later.
        /// </summary>
        public Saga<T> ExpectHandleCurrentMessageLater()
        {
            expectedInvocations.Add(new ExpectedHandleCurrentMessageLaterInvocation<object>());
            return this;
        }

        /// <summary>
        /// Uses the given delegate to invoke the saga, checking all the expectations previously set up,
        /// and then clearing them for continued testing.
        /// </summary>
        public Saga<T> When(Action<T> sagaIsInvoked)
        {
            var id = messageId();

            var context = new MessageContext { Id = id, ReturnAddress = saga.Entity.Originator, Headers = incomingHeaders };
            bus.CurrentMessageContext = context;

            sagaIsInvoked(saga);

            bus.ValidateAndReset(expectedInvocations);
            expectedInvocations.Clear();

            messageId = () => Guid.NewGuid().ToString();
            return this;
        }

        /// <summary>
        /// Invokes the saga timeout passing in the last timeout state it sent
        /// and then clears out all previous expectations.
        /// </summary>
        public Saga<T> WhenSagaTimesOut()
        {
            var state = bus.PopTimeout();
            
            var method = saga.GetType().GetMethod("Timeout", new[] {state.GetType()});
            
            return When(s => method.Invoke(s, new[] {state}));
        }

        /// <summary>
        /// Asserts that the saga is either complete or not.
        /// </summary>
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
        public Saga<T> ExpectTimeoutToBeSetIn<TMessage>(Func<TMessage, TimeSpan, bool> check = null)
        {
            expectedInvocations.Add(new ExpectedDeferMessageInvocation<TMessage, TimeSpan> { Check = check });
            return this;
        }

        /// <summary>
        /// Verifies that the saga is not setting the specified timeout
        /// </summary>
        public Saga<T> ExpectNoTimeoutToBeSetIn<TMessage>(Func<TMessage, TimeSpan, bool> check = null)
        {
            expectedInvocations.Add(new ExpectedNotDeferMessageInvocation<TMessage, TimeSpan> { Check = check });
            return this;
        }

        /// <summary>
        /// Verifies that the saga is setting the specified timeout
        /// </summary>
        public Saga<T> ExpectTimeoutToBeSetIn<TMessage>(Action<TMessage, TimeSpan> check)
        {
            return ExpectTimeoutToBeSetIn(CheckActionToFunc(check));
        }

        /// <summary>
        /// Verifies that the saga is setting the specified timeout
        /// </summary>
        public Saga<T> ExpectTimeoutToBeSetAt<TMessage>(Func<TMessage, DateTime, bool> check = null)
        {
            expectedInvocations.Add(new ExpectedDeferMessageInvocation<TMessage, DateTime> { Check = check });
            return this;
        }

        /// <summary>
        /// Verifies that the saga is not setting the specified timeout
        /// </summary>
        public Saga<T> ExpectNoTimeoutToBeSetAt<TMessage>(Func<TMessage, DateTime, bool> check = null)
        {
            expectedInvocations.Add(new ExpectedNotDeferMessageInvocation<TMessage, DateTime> { Check = check });
            return this;
        }

        /// <summary>
        /// Verifies that the saga is setting the specified timeout
        /// </summary>
        public Saga<T> ExpectTimeoutToBeSetAt<TMessage>(Action<TMessage, DateTime> check)
        {
            return ExpectTimeoutToBeSetAt(CheckActionToFunc(check));
        }

        private static Func<T1, bool> CheckActionToFunc<T1>(Action<T1> check)
        {
            return arg =>
            {
                check(arg);
                return true;
            };
        }
        
        private static Func<T1, T2, bool> CheckActionToFunc<T1, T2>(Action<T1, T2> check)
        {
            return (arg1, arg2) =>
            {
                check(arg1, arg2);
                return true;
            };
        }
    }
}
