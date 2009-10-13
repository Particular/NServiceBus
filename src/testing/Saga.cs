using System;
using System.Collections.Generic;
using System.Reflection;
using NServiceBus.Saga;
using Rhino.Mocks;

namespace NServiceBus.Testing
{
    /// <summary>
    /// Entry class used for unit testing sagas.
    /// </summary>
    public static class Saga
    {
        /// <summary>
        /// Initializes the testing infrastructure.
        /// </summary>
        public static void Initialize()
        {
            var mapper = new NServiceBus.MessageInterfaces.MessageMapper.Reflection.MessageMapper();
            messageTypes = new List<Type>();
            NServiceBus.Configure.With();
            
            foreach (var t in Configure.TypesToScan)
                if (typeof(IMessage).IsAssignableFrom(t))
                    if (!messageTypes.Contains(t))
                        messageTypes.Add(t);

            mapper.Initialize(messageTypes.ToArray());

            messageCreator = mapper;

            ExtensionMethods.MessageCreator = messageCreator;
        }

        /// <summary>
        /// Begin the test script for a saga of type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Saga<T> Test<T>() where T : ISaga, new()
        {
            return Test<T>(Guid.NewGuid());
        }

        /// <summary>
        /// Begin the test script for a saga of type T while specifying the saga id.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Saga<T> Test<T>(Guid sagaId) where T : ISaga, new()
        {
            if (messageCreator == null)
                throw new InvalidOperationException("Please call 'Initialize' before calling this method.");

            var saga = (T)Activator.CreateInstance(typeof(T));

            var prop = typeof(T).GetProperty("Data");
            var sagaData = Activator.CreateInstance(prop.PropertyType) as ISagaEntity;

            saga.Entity = sagaData;

            saga.Entity.Id = sagaId;

            var mocks = new MockRepository();
            var bus = mocks.DynamicMock<IBus>();

            saga.Bus = bus;

            return new Saga<T>(saga, mocks, bus, messageCreator, messageTypes);
        }

        /// <summary>
        /// Instantiate a new message of type M.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static M CreateInstance<M>() where M : IMessage
        {
            return messageCreator.CreateInstance<M>();
        }

        /// <summary>
        /// Instantiate a new message of type T performing the given action
        /// on the created message.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <returns></returns>
        public static M CreateInstance<M>(Action<M> action) where M : IMessage
        {
            return messageCreator.CreateInstance(action);
        }

        private static IMessageCreator messageCreator;
        private static List<Type> messageTypes;
    }

    /// <summary>
    /// Saga unit testing framework.
    /// </summary>
    public class Saga<T> where T : ISaga, new()
    {
        private readonly IBus bus;
        private readonly T saga;
        private readonly MockRepository m;
        private readonly IMessageCreator messageCreator;
        private string messageId;
        private string clientAddress;
        private readonly List<Delegate> delegates = new List<Delegate>();
        private readonly List<Type> messageTypes = new List<Type>();
        private IDictionary<string, string> incomingHeaders = new Dictionary<string, string>();
        private IDictionary<string, string> outgoingHeaders = new Dictionary<string, string>();

        public Saga(T saga, MockRepository mocks, IBus b, IMessageCreator messageCreator, List<Type> types)
        {
            this.saga = saga;
            m = mocks;
            bus = b;
            this.messageCreator = messageCreator;
            messageTypes = types;
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
        /// <param name="headers"></param>
        /// <returns></returns>
        public Saga<T> SetIncomingHeaders(IDictionary<string, string> headers)
        {
            incomingHeaders = headers;

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
        /// <typeparam name="T"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Saga<T> ExpectSend<M>(SendPredicate<M> check) where M : IMessage
        {
            Delegate d = new HandleMessageDelegate(
                () => ExpectCallToSend(
                          delegate(IMessage[] msgs)
                              {
                                  foreach (M msg in msgs)
                                      if (!check(msg))
                                          return false;

                                  return true;
                              }
                          )
                );

            delegates.Add(d);
            return this;
        }

        /// <summary>
        /// Check that the saga replies with the given message type complying with the given predicate.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Saga<T> ExpectReply<M>(SendPredicate<M> check) where M : IMessage
        {
            Delegate d = new HandleMessageDelegate(
                () => ExpectCallToReply(
                          delegate(IMessage[] msgs)
                              {
                                  foreach (M msg in msgs)
                                      if (!check(msg))
                                          return false;

                                  return true;
                              }
                          )
                );

            delegates.Add(d);
            return this;
        }

        /// <summary>
        /// Check that the saga sends the given message type to its local queue
        /// and that the message complies with the given predicate.
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Saga<T> ExpectSendLocal<M>(SendPredicate<M> check) where M : IMessage
        {
            Delegate d = new HandleMessageDelegate(
                () => ExpectCallToSendLocal(
                          delegate(IMessage[] msgs)
                              {
                                  foreach (M msg in msgs)
                                      if (!check(msg))
                                          return false;

                                  return true;
                              }
                          )
                );

            delegates.Add(d);
            return this;
        }

        /// <summary>
        /// Check that the saga uses the bus to return the appropriate error code.
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        public Saga<T> ExpectReturn(ReturnPredicate check)
        {
            Delegate d = new HandleMessageDelegate(
                () => ExpectCallToReturn(check)
                );

            delegates.Add(d);
            return this;
        }

        /// <summary>
        /// Check that the saga sends the given message type to the appropriate destination.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Saga<T> ExpectSendToDestination<M>(SendToDestinationPredicate<M> check) where M : IMessage
        {
            Delegate d = new HandleMessageDelegate(
                () => ExpectCallToSend(
                          delegate(string destination, IMessage[] msgs)
                              {
                                  foreach (M msg in msgs)
                                      if (!check(destination, msg))
                                          return false;

                                  return true;
                              }
                          )
                );

            delegates.Add(d);
            return this;
        }

        /// <summary>
        /// Check that the saga replies to the originator with the given message type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Saga<T> ExpectReplyToOrginator<M>(SendPredicate<M> check) where M : IMessage
        {
            Delegate d = new HandleMessageDelegate(
                () => ExpectCallToSend(
                          delegate(string destination, string correlationId, IMessage[] msgs)
                              {
                                  foreach (M msg in msgs)
                                      if (!check(msg))
                                          return false;

                                  return true;
                              }
                          )
                );

            delegates.Add(d);
            return this;
        }

        /// <summary>
        /// Check that the saga publishes a message of the given type complying with the given predicate.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public Saga<T> ExpectPublish<M>(PublishPredicate<M> check) where M : IMessage
        {
            Delegate d = new HandleMessageDelegate(
                () => ExpectCallToPublish(
                          delegate(M[] msgs)
                              {
                                  foreach (M msg in msgs)
                                      if (!check(msg))
                                          return false;

                                  return true;
                              }
                          )
                );

            delegates.Add(d);
            return this;
        }

        /// <summary>
        /// Uses the given delegate to invoke the saga, checking all the expectations previously set up,
        /// and then clearing them for continued testing.
        /// </summary>
        /// <param name="handle"></param>
        public Saga<T> When(Action<T> sagaIsInvoked)
        {
            var context = new MessageContext { Id = messageId, ReturnAddress = clientAddress, Headers = incomingHeaders };

            using (m.Record())
            {
                foreach (var t in messageTypes)
                    GetType().GetMethod("PrepareBusGenericMethods", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(t).Invoke(this, null);

                SetupResult.For(bus.CurrentMessageContext).Return(context);

                foreach (var d in delegates)
                    d.DynamicInvoke();
            }

            using (m.Playback())
                sagaIsInvoked(saga);

            m.BackToRecordAll();

            delegates.Clear();

            return this;
        }

        public Saga<T> AssertSagaCompletionIs(bool complete)
        {
            if (saga.Completed == complete)
                return this;

            if (saga.Completed)
                throw new Exception("Assert failed. Saga has been completed.");
            else
                throw new Exception("Assert failed. Saga has not been completed.");
        }

        private void ExpectCallToReturn(ReturnPredicate callback)
        {
            Expect.Call(delegate { bus.Return(-1); })
                .IgnoreArguments()
                .Callback(callback);
        }

        private void ExpectCallToReply(BusSendDelegate callback)
        {
            IMessage[] messages = null;

            Expect.Call(delegate { bus.Reply(messages); })
                .IgnoreArguments()
                .Callback(callback);
        }

        private void ExpectCallToSendLocal(BusSendDelegate callback)
        {
            IMessage[] messages = null;

            Expect.Call(delegate { bus.SendLocal(messages); })
                .IgnoreArguments()
                .Callback(callback);
        }

        private void ExpectCallToSend(BusSendDelegate callback)
        {
            IMessage[] messages = null;

            Expect.Call(delegate { bus.Send(messages); })
                .IgnoreArguments().Return(null)
                .Callback(callback);
        }

        private void ExpectCallToSend(BusSendWithDestinationDelegate callback)
        {
            IMessage[] messages = null;
            string destination = null;

            Expect.Call(delegate { bus.Send(destination, messages); })
                .IgnoreArguments().Return(null)
                .Callback(callback);
        }

        private void ExpectCallToSend(BusSendWithDestinationAndCorrelationIdDelegate callback)
        {
            IMessage[] messages = null;
            string destination = null;
            string correlationId = null;

            Expect.Call(delegate { bus.Send(destination, correlationId, messages); })
                .IgnoreArguments()
                .Callback(callback);
        }

        private void ExpectCallToPublish<T>(BusPublishDelegate<T> callback) where T : IMessage
        {
            T[] messages = null;

            Expect.Call(delegate { bus.Publish(messages); })
                .IgnoreArguments()
                .Callback(callback);
        }

        private void PrepareBusGenericMethods<M>() where M : IMessage
        {
            Delegate d = new HandleMessageDelegate(
                delegate
                {
                    Action<M> act = null;
                    string destination = null;

                    bus.CreateInstance<M>();
                    LastCall.Repeat.Any().IgnoreArguments().Return(null).WhenCalled(mi =>
                    {
                        mi.ReturnValue = this.messageCreator.CreateInstance<M>();
                    }
                    );

                    bus.CreateInstance<M>(act);
                    LastCall.Repeat.Any().IgnoreArguments().Return(null).WhenCalled(mi =>
                    {
                        Action<M> action = mi.Arguments[0] as Action<M>;
                        mi.ReturnValue = this.messageCreator.CreateInstance<M>(action);
                    }
                    );

                    bus.Reply<M>(act);
                    LastCall.Repeat.Any().IgnoreArguments().WhenCalled(mi =>
                    {
                        Action<M> action = mi.Arguments[0] as Action<M>;
                        bus.Reply(this.messageCreator.CreateInstance<M>(action));
                    }
                    );

                    bus.Send<M>(act);
                    LastCall.Repeat.Any().IgnoreArguments().Return(null).WhenCalled(mi =>
                    {
                        Action<M> action = mi.Arguments[0] as Action<M>;
                        bus.Send(this.messageCreator.CreateInstance<M>(action));
                    }
                    );
                    
                    bus.Send<M>(destination, act);
                    LastCall.Repeat.Any().IgnoreArguments().Return(null).WhenCalled(mi =>
                    {
                        string dest = mi.Arguments[0] as string;
                        Action<M> action = mi.Arguments[1] as Action<M>;
                        bus.Send(dest, this.messageCreator.CreateInstance<M>(action));
                    }
                    );

                    bus.SendLocal<M>(act);
                    LastCall.Repeat.Any().IgnoreArguments().WhenCalled(mi =>
                    {
                        Action<M> action = mi.Arguments[0] as Action<M>;
                        bus.SendLocal(this.messageCreator.CreateInstance<M>(action));
                    }
                    );

                    bus.Publish<M>(act);
                    LastCall.Repeat.Any().IgnoreArguments().WhenCalled(mi =>
                    {
                        Action<M> action = mi.Arguments[0] as Action<M>;
                        bus.Publish(this.messageCreator.CreateInstance<M>(action));
                    }
                    );

                }
            );

            this.delegates.Add(d);
        }

    }

}
