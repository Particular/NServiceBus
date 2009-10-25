using System;
using System.Collections.Generic;
using System.Linq;
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
            var mapper = new MessageInterfaces.MessageMapper.Reflection.MessageMapper();
            messageTypes = new List<Type>();
            
            Configure.With();

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
            if (messageCreator == null)
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

            return new Saga<T>(saga, mocks, bus, messageCreator, messageTypes);
        }

        /// <summary>
        /// Begin the test script for a saga of type T while specifying the saga id.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TMessage"></typeparam>
        /// <returns></returns>
        public static Handler<T, TMessage> Handler<T, TMessage>()
            where TMessage : IMessage, new()
            where T : IMessageHandler<TMessage>, new()
        {
            if (messageCreator == null)
                throw new InvalidOperationException("Please call 'Initialize' before calling this method.");

            var handler = (T)Activator.CreateInstance(typeof(T));

            var mocks = new MockRepository();
            var bus = mocks.DynamicMock<IBus>();

            var prop = typeof(T).GetProperties().Where(p => p.PropertyType == typeof(IBus)).FirstOrDefault();
            if (prop != null)
                prop.SetValue(handler, bus, null);

            ExtensionMethods.Bus = bus;

            return new Handler<T, TMessage>(handler, mocks, bus, messageCreator, messageTypes);
        }

        /// <summary>
        /// Instantiate a new message of type TMessage.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <returns></returns>
        public static TMessage CreateInstance<TMessage>() where TMessage : IMessage
        {
            return messageCreator.CreateInstance<TMessage>();
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
            return messageCreator.CreateInstance(action);
        }

        /// <summary>
        /// Returns the message creator.
        /// </summary>
        private static IMessageCreator messageCreator;

        private static List<Type> messageTypes;
    }
}
