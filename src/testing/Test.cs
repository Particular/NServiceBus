using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NServiceBus.Saga;
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
            var mapper = new MessageInterfaces.MessageMapper.Reflection.MessageMapper();
            _messageTypes = new List<Type>();

            foreach (var t in Configure.TypesToScan)
                if (typeof(IMessage).IsAssignableFrom(t))
                    if (!_messageTypes.Contains(t))
                        _messageTypes.Add(t);

            mapper.Initialize(_messageTypes.ToArray());

            _messageCreator = mapper;

            ExtensionMethods.MessageCreator = _messageCreator;
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
            if (_messageCreator == null)
                throw new InvalidOperationException("Please call 'Initialize' before calling this method.");

            var saga = (T)Activator.CreateInstance(typeof(T));

            var prop = typeof(T).GetProperty("Data");
            var sagaData = Activator.CreateInstance(prop.PropertyType) as ISagaEntity;

            saga.Entity = sagaData;

            if (saga.Entity != null) saga.Entity.Id = sagaId;

            var mocks = new MockRepository();
            var bus = mocks.DynamicMock<IBus>();

            saga.Bus = bus;
            ExtensionMethods.Bus = bus;

            return new Saga<T>(saga, mocks, bus, _messageCreator, _messageTypes);
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
            if (_messageCreator == null)
                throw new InvalidOperationException("Please call 'Initialize' before calling this method.");

            bool isHandler = false;
            foreach(var i in handler.GetType().GetInterfaces())
            {
                var args = i.GetGenericArguments();
                if (args.Length == 1)
                    if (typeof(IMessageHandler<>).MakeGenericType(args[0]).IsAssignableFrom(i))
                        isHandler = true;
            }

            if (!isHandler)
                throw new ArgumentException("The handler object given does not implement IMessageHandler<T> where T : IMessage.", "handler");

            var mocks = new MockRepository();
            var bus = mocks.DynamicMock<IBus>();

            var prop = typeof(T).GetProperties().Where(p => p.PropertyType == typeof(IBus)).FirstOrDefault();
            if (prop != null)
                prop.SetValue(handler, bus, null);

            ExtensionMethods.Bus = bus;

            return new Handler<T>(handler, mocks, bus, _messageCreator, _messageTypes);
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

        private static List<Type> _messageTypes;
    }
}
