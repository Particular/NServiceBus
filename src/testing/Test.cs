using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NServiceBus.Saga;
using NServiceBus.Serialization;
using NServiceBus.Unicast;
using Rhino.Mocks;

namespace NServiceBus.Testing
{
    /// <summary>
    /// Entry class used for unit testing
    /// </summary>
    public static class Test
    {
        /// <summary>
        /// Initializes the testing infrastructure.
        /// </summary>
        public static void Initialize()
        {
            Configure.With();
            InitializeInternal();
        }

        /// <summary>
        /// Initializes the testing infrastructure specifying which assemblies to scan.
        /// </summary>
        public static void Initialize(params Assembly[] assemblies)
        {
            Configure.With(assemblies);
            InitializeInternal();
        }

        private static void InitializeInternal()
        {
            Configure.Instance
                .DefaultBuilder()
                .XmlSerializer()
                .InMemoryFaultManagement();
        }

        /// <summary>
        /// Begin the test script for a saga of type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Saga<T> Saga<T>() where T : ISaga, new()
        {
            return Saga<T>(Guid.NewGuid());
        }

        /// <summary>
        /// Begin the test script for a saga of type T while specifying the saga id.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Saga<T> Saga<T>(Guid sagaId) where T : ISaga, new()
        {
            var saga = (T)Activator.CreateInstance(typeof(T));

            var prop = typeof(T).GetProperty("Data");
            var sagaData = Activator.CreateInstance(prop.PropertyType) as ISagaEntity;

            saga.Entity = sagaData;

            if (saga.Entity != null) saga.Entity.Id = sagaId;

            return Saga(saga);
        }

        /// <summary>
        /// Begin the test script for the passed in saga instance.
        /// Callers need to instantiate the saga's data class as well as give it an ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="saga"></param>
        /// <returns></returns>
        public static Saga<T> Saga<T>(T saga) where T : ISaga, new()
        {
            var mocks = new MockRepository();
            var bus = MockTheBus(mocks);

            saga.Bus = bus;

            var messageTypes = Configure.TypesToScan.Where(t => typeof(IMessage).IsAssignableFrom(t)).ToList();

            return new Saga<T>(saga, mocks, bus, _messageCreator, messageTypes);
        }

        /// <summary>
        /// Specify a test for a message handler of type T for a given message of type TMessage.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Handler<T> Handler<T>() where T : new()
        {
            var handler = (T)Activator.CreateInstance(typeof(T));

            return Handler(handler);
        }

        /// <summary>
        /// Specify a test for a message handler while supplying the instance to
        /// test - useful if you use constructor-based dependency injection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Handler<T> Handler<T>(T handler)
        {
            bool isHandler = false;
            foreach(var i in handler.GetType().GetInterfaces())
            {
                var args = i.GetGenericArguments();
                if (args.Length == 1)
                    if (typeof(IMessage).IsAssignableFrom(args[0]))
                        if (typeof(IMessageHandler<>).MakeGenericType(args[0]).IsAssignableFrom(i))
                            isHandler = true;
            }

            if (!isHandler)
                throw new ArgumentException("The handler object given does not implement IMessageHandler<T> where T : IMessage.", "handler");

            var mocks = new MockRepository();
            var bus = MockTheBus(mocks);

            var prop = typeof(T).GetProperties().Where(p => p.PropertyType == typeof(IBus)).FirstOrDefault();
            if (prop != null)
                prop.SetValue(handler, bus, null);

            var messageTypes = Configure.TypesToScan.Where(t => typeof(IMessage).IsAssignableFrom(t)).ToList();

            return new Handler<T>(handler, mocks, bus, _messageCreator, messageTypes);
        }

        private static IUnicastBus MockTheBus(MockRepository mocks)
        {
            var bus = mocks.DynamicMock<IUnicastBus>();
            var starter = mocks.DynamicMock<IStartableBus>();

            Configure.Instance.Configurer.RegisterSingleton(typeof(IStartableBus), starter);
            Configure.Instance.Configurer.RegisterSingleton(typeof(IUnicastBus), bus);

            bus.Replay(); // to neutralize any event subscriptions by rest of NSB
            
            ExtensionMethods.Bus = bus;

            Configure.Instance.CreateBus();

            Configure.Instance.Builder.Build<IMessageSerializer>(); // needed to pass message types to message creator
            _messageCreator = Configure.Instance.Builder.Build<IMessageCreator>();
            if (_messageCreator == null)
                throw new InvalidOperationException("Please call 'Initialize' before calling this method.");

            bus.BackToRecord(); // to get ready for testing

            return bus;
        }

        /// <summary>
        /// Instantiate a new message of type TMessage.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <returns></returns>
        public static TMessage CreateInstance<TMessage>() where TMessage : IMessage
        {
            return _messageCreator.CreateInstance<TMessage>();
        }

        /// <summary>
        /// Instantiate a new message of type TMessage performing the given action
        /// on the created message.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="action"></param>
        /// <returns></returns>
        public static TMessage CreateInstance<TMessage>(Action<TMessage> action) where TMessage : IMessage
        {
            return _messageCreator.CreateInstance(action);
        }

        /// <summary>
        /// Returns the message creator.
        /// </summary>
        private static IMessageCreator _messageCreator;
    }
}
