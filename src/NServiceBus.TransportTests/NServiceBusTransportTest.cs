namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using DeliveryConstraints;
    using Extensibility;
    using Logging;
    using NUnit.Framework;
    using Routing;
    using Settings;
    using Transport;

    public abstract class NServiceBusTransportTest
    {
        [SetUp]
        public void SetUp()
        {
            testId = Guid.NewGuid().ToString();

            LogManager.UseFactory(new TransportTestLoggerFactory());

            //when using [TestCase] NUnit will reuse the same test instance so we need to make sure that the message pump is a fresh one
            MessagePump = null;
        }

        static IConfigureTransportInfrastructure CreateConfigurer()
        {
            var transportToUse = EnvironmentHelper.GetEnvironmentVariable("Transport.UseSpecific");

            if (string.IsNullOrWhiteSpace(transportToUse))
            {
                var coreAssembly = typeof(IMessage).Assembly;

                var nonCoreTransport = transportDefinitions.Value.FirstOrDefault(t => t.Assembly != coreAssembly);

                transportToUse = nonCoreTransport?.Name ?? DefaultTransportDescriptorKey;
            }

            var typeName = $"Configure{transportToUse}Infrastructure";

            var configurerType = Type.GetType(typeName, false);

            if (configurerType == null)
            {
                throw new InvalidOperationException($"Transport Test project must include a non-namespaced class named '{typeName}' implementing {typeof(IConfigureTransportInfrastructure).Name}.");
            }

            var configurer = Activator.CreateInstance(configurerType) as IConfigureTransportInfrastructure;

            if (configurer == null)
            {
                throw new InvalidOperationException($"{typeName} does not implement {typeof(IConfigureTransportInfrastructure).Name}.");
            }

            return configurer;
        }

        [TearDown]
        public void TearDown()
        {
            testCancellationTokenSource?.Dispose();
            MessagePump?.Stop().GetAwaiter().GetResult();
            Configurer?.Cleanup().GetAwaiter().GetResult();

            transportSettings.Clear();
        }

        protected async Task StartPump(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, TransportTransactionMode transactionMode, Action<string, Exception> onCriticalError = null)
        {
            InputQueueName = GetTestName() + transactionMode;
            ErrorQueueName = $"{InputQueueName}.error";

            transportSettings.Set("NServiceBus.Routing.EndpointName", InputQueueName);

            var queueBindings = new QueueBindings();
            queueBindings.BindReceiving(InputQueueName);
            queueBindings.BindSending(ErrorQueueName);
            transportSettings.Set(ErrorQueueSettings.SettingsKey, ErrorQueueName);
            transportSettings.Set<QueueBindings>(queueBindings);

            transportSettings.Set<EndpointInstances>(new EndpointInstances());

            Configurer = CreateConfigurer();

            var configuration = Configurer.Configure(transportSettings, transactionMode);

            TransportInfrastructure = configuration.TransportInfrastructure;

            IgnoreUnsupportedTransactionModes(transactionMode);
            IgnoreUnsupportedDeliveryConstraints();

            ReceiveInfrastructure = TransportInfrastructure.ConfigureReceiveInfrastructure();
            SendInfrastructure = TransportInfrastructure.ConfigureSendInfrastructure();

            lazyDispatcher = new Lazy<IDispatchMessages>(() => SendInfrastructure.DispatcherFactory());

            MessagePump = ReceiveInfrastructure.MessagePumpFactory();

            var queueCreator = ReceiveInfrastructure.QueueCreatorFactory();
            var userName = GetUserName();
            await queueCreator.CreateQueueIfNecessary(queueBindings, userName);

            var pushSettings = new PushSettings(InputQueueName, ErrorQueueName, configuration.PurgeInputQueueOnStartup, transactionMode);
            await MessagePump.Init(
                context =>
                {
                    if (context.Headers.ContainsKey(TestIdHeaderName) && context.Headers[TestIdHeaderName] == testId)
                    {
                        return onMessage(context);
                    }

                    return Task.FromResult(0);
                },
                context =>
                {
                    if (context.Message.Headers.ContainsKey(TestIdHeaderName) && context.Message.Headers[TestIdHeaderName] == testId)
                    {
                        return onError(context);
                    }

                    return Task.FromResult(ErrorHandleResult.Handled);
                },
                new FakeCriticalError(onCriticalError),
                pushSettings);

            MessagePump.Start(configuration.PushRuntimeSettings);
        }

        string GetUserName()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return $"{Environment.UserDomainName}\\{Environment.UserName}";
            }

            return Environment.UserName;
        }

        void IgnoreUnsupportedDeliveryConstraints()
        {
            var supportedDeliveryConstraints = TransportInfrastructure.DeliveryConstraints.ToList();
            var unsupportedDeliveryConstraints = requiredDeliveryConstraints.Where(required => !supportedDeliveryConstraints.Contains(required))
                .ToList();

            if (unsupportedDeliveryConstraints.Any())
            {
                var unsupported = string.Join(",", unsupportedDeliveryConstraints.Select(c => c.Name));
                Assert.Ignore($"Transport doesn't support required delivery constraint(s) {unsupported}");
            }
        }

        void IgnoreUnsupportedTransactionModes(TransportTransactionMode requestedTransactionMode)
        {
            if (TransportInfrastructure.TransactionMode < requestedTransactionMode)
            {
                Assert.Ignore($"Only relevant for transports supporting {requestedTransactionMode} or higher");
            }
        }

        protected Task SendMessage(string address,
            Dictionary<string, string> headers = null,
            TransportTransaction transportTransaction = null,
            List<DeliveryConstraint> deliveryConstraints = null,
            DispatchConsistency dispatchConsistency = DispatchConsistency.Default)
        {
            var messageId = Guid.NewGuid().ToString();
            var message = new OutgoingMessage(messageId, headers ?? new Dictionary<string, string>(), new byte[0]);

            if (message.Headers.ContainsKey(TestIdHeaderName) == false)
            {
                message.Headers.Add(TestIdHeaderName, testId);
            }

            var dispatcher = lazyDispatcher.Value;

            if (transportTransaction == null)
            {
                transportTransaction = new TransportTransaction();
            }

            var transportOperation = new TransportOperation(message, new UnicastAddressTag(address), dispatchConsistency, deliveryConstraints ?? new List<DeliveryConstraint>());

            return dispatcher.Dispatch(new TransportOperations(transportOperation), transportTransaction, new ContextBag());
        }

        protected void OnTestTimeout(Action onTimeoutAction)
        {
            testCancellationTokenSource = Debugger.IsAttached ? new CancellationTokenSource() : new CancellationTokenSource(TimeSpan.FromSeconds(30));

            testCancellationTokenSource.Token.Register(onTimeoutAction);
        }

        protected void RequireDeliveryConstraint<T>() where T : DeliveryConstraint
        {
            requiredDeliveryConstraints.Add(typeof(T));
        }

        static string GetTestName()
        {
            var index = 1;
            var frame = new StackFrame(index);
            Type type;

            while (true)
            {
                type = frame.GetMethod().DeclaringType;

                if (type != null && !type.IsAbstract && typeof(NServiceBusTransportTest).IsAssignableFrom(type))
                {
                    break;
                }

                frame = new StackFrame(++index);
            }

            var classCallingUs = type.FullName.Split('.').Last();

            var testName = classCallingUs.Split('+').First();

            testName = testName.Replace("When_", "");

            testName = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(testName);

            testName = testName.Replace("_", "");

            return testName;
        }

        protected string InputQueueName;
        protected string ErrorQueueName;

        string testId;

        List<Type> requiredDeliveryConstraints = new List<Type>();
        SettingsHolder transportSettings = new SettingsHolder();
        Lazy<IDispatchMessages> lazyDispatcher;
        TransportReceiveInfrastructure ReceiveInfrastructure;
        TransportSendInfrastructure SendInfrastructure;
        TransportInfrastructure TransportInfrastructure;
        IPushMessages MessagePump;
        CancellationTokenSource testCancellationTokenSource;
        IConfigureTransportInfrastructure Configurer;

        const string DefaultTransportDescriptorKey = "LearningTransport";
        const string TestIdHeaderName = "TransportTest.TestId";

        static Lazy<List<Type>> transportDefinitions = new Lazy<List<Type>>(() => TypeScanner.GetAllTypesAssignableTo<TransportDefinition>().ToList());

        class FakeCriticalError : CriticalError
        {
            public FakeCriticalError(Action<string, Exception> errorAction) : base(null)
            {
                this.errorAction = errorAction ?? ((s, e) => { });
            }

            public override void Raise(string errorMessage, Exception exception)
            {
                errorAction(errorMessage, exception);
            }

            Action<string, Exception> errorAction;
        }

        class EnvironmentHelper
        {
            public static string GetEnvironmentVariable(string variable)
            {
                var candidate = Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User);

                if (string.IsNullOrWhiteSpace(candidate))
                {
                    return Environment.GetEnvironmentVariable(variable);
                }

                return candidate;
            }
        }
    }
}