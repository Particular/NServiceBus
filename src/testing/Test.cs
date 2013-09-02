using System;
using System.Linq;
using System.Reflection;
using NServiceBus.MessageInterfaces;
using NServiceBus.Saga;
using NServiceBus.Config.ConfigurationSource;

namespace NServiceBus.Testing
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Features.Categories;
    using DataBus.InMemory;
    using Persistence.InMemory.SagaPersister;

    /// <summary>
    /// Entry class used for unit testing
    /// </summary>
    public static class Test
    {
        /// <summary>
        /// Get the reference to the bus used for testing.
        /// </summary>
        public static IBus Bus { get { return bus; } }
        [ThreadStatic] private static StubBus bus;

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
        public static void Initialize(IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null)
                throw new ArgumentNullException("assemblies");
            
            Initialize(assemblies.ToArray());
        }
        
        /// <summary>
        /// Initializes the testing infrastructure specifying which assemblies to scan.
        /// </summary>
        public static void Initialize(params Assembly[] assemblies)
        {
            Configure.With(assemblies);
            InitializeInternal();
        }

        /// <summary>
        /// Initializes the testing infrastructure specifying which types to scan.
        /// </summary>
        public static void Initialize(params Type[] types)
        {
            Configure.With(types);
            InitializeInternal();
        }

        private static void InitializeInternal()
        {
            if (initialized)
                return;

            Serializers.SetDefault<Features.XmlSerialization>();

            Configure.Instance
                .DefineEndpointName("UnitTests")
                 .CustomConfigurationSource(testConfigurationSource)
                .DefaultBuilder()
                .UseInMemoryGatewayPersister()
                .UseInMemoryTimeoutPersister()
                .InMemoryFaultManagement()
                .InMemorySubscriptionStorage()
                .UnicastBus();

            Configure.Component<InMemoryDataBus>(DependencyLifecycle.SingleInstance);

            Configure.Instance.Initialize();

            
            var mapper = Configure.Instance.Builder.Build<IMessageMapper>();
            if (mapper == null)
                throw new InvalidOperationException("Please call 'Initialize' before calling this method.");

            //mapper.Initialize(Configure.TypesToScan.Where(MessageConventionExtensions.IsMessageType));
            
            messageCreator = mapper;
            ExtensionMethods.MessageCreator = messageCreator;
            ExtensionMethods.GetHeaderAction = (msg, key) =>
                {
                    ConcurrentDictionary<string, string> kv;
                    if (messageHeaders.TryGetValue(msg, out kv))
                    {
                        string val;
                        if (kv.TryGetValue(key, out val))
                        {
                            return val;
                        }
                    }

                    return null;
                };
            ExtensionMethods.SetHeaderAction = (msg, key, val) =>
                    messageHeaders.AddOrUpdate(msg, 
                        o => new ConcurrentDictionary<string, string>(new[]{new KeyValuePair<string, string>(key, val)}), 
                        (o, dic) =>
                            {
                                dic.AddOrUpdate(key, val, (s, s1) => val);
                                return dic;
                            });
                
            ExtensionMethods.GetStaticOutgoingHeadersAction = () => staticOutgoingHeaders;

            initialized = true;
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
            var sagaData = Activator.CreateInstance(prop.PropertyType) as IContainSagaData;

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
            bus = new StubBus(messageCreator);
            ExtensionMethods.Bus = bus;

            saga.Bus = Bus;

            return new Saga<T>(saga, bus);
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
        /// test - injects the bus into a public property (if it exists).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Handler<T> Handler<T>(T handler)
        {
            Func<IBus, T> handlerCreator = b => handler;
            var prop = typeof(T).GetProperties().Where(p => p.PropertyType == typeof(IBus)).FirstOrDefault();
            if (prop != null)
                handlerCreator = b =>
                                     {
                                         prop.SetValue(handler, b, null);
                                         return handler;
                                     };

            return Handler(handlerCreator);
        }

        /// <summary>
        /// Specify a test for a message handler specifying a callback to create
        /// the handler and getting an instance of the bus passed in.
        /// Useful for handlers based on constructor injection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handlerCreationCallback"></param>
        /// <returns></returns>
        public static Handler<T> Handler<T>(Func<IBus, T> handlerCreationCallback)
        {
            bus = new StubBus(messageCreator);
            ExtensionMethods.Bus = bus;

            var handler = handlerCreationCallback.Invoke(bus);

            bool isHandler = (from i in handler.GetType().GetInterfaces()
                              let args = i.GetGenericArguments()
                              where args.Length == 1
                              where MessageConventionExtensions.IsMessageType(args[0])
                              where typeof(IHandleMessages<>).MakeGenericType(args[0]).IsAssignableFrom(i)
                              select i).Any();

            if (!isHandler)
                throw new ArgumentException("The handler object created does not implement IHandleMessages<T>.", "handlerCreationCallback");

            var messageTypes = Configure.TypesToScan.Where(MessageConventionExtensions.IsMessageType).ToList();

            return new Handler<T>(handler, bus, messageCreator, messageTypes);
        }

        /// <summary>
        /// Instantiate a new message of type TMessage.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <returns></returns>
        public static TMessage CreateInstance<TMessage>()
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
        public static TMessage CreateInstance<TMessage>(Action<TMessage> action)
        {
            return messageCreator.CreateInstance(action);
        }

        static IMessageCreator messageCreator;
        static readonly TestConfigurationSource testConfigurationSource = new TestConfigurationSource();
        static readonly ConcurrentDictionary<object, ConcurrentDictionary<string, string>> messageHeaders = new ConcurrentDictionary<object, ConcurrentDictionary<string, string>>();
        static readonly IDictionary<string, string> staticOutgoingHeaders = new Dictionary<string, string>();
        static bool initialized;
    }

    /// <summary>
    /// Configuration source suitable for testing
    /// </summary>
    public class TestConfigurationSource:IConfigurationSource
    {
        /// <summary>
        /// Returns null for all types of T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetConfiguration<T>() where T : class, new()
        {
            return null;
        }
    }
}
