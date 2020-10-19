namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration.AdvancedExtensibility;
    using DeliveryConstraints;
    using Extensibility;
    using Logging;
    using NUnit.Framework;
    using Routing;
    using Settings;
    using Transport;

    public abstract class NServiceBusTransportTest
    {
        static NServiceBusTransportTest()
        {
            LogFactory = new TransportTestLoggerFactory();
            LogManager.UseFactory(LogFactory);
        }

        [SetUp]
        public void SetUp()
        {
            testId = Guid.NewGuid().ToString();

            LogFactory.LogItems.Clear();

            //when using [TestCase] NUnit will reuse the same test instance so we need to make sure that the message pump is a fresh one
            MessagePump = null;
            TransportInfrastructure = null;
            Configurer = null;
            testCancellationTokenSource = null;
        }

        static IConfigureTransportInfrastructure CreateConfigurer()
        {
            var transportToUse = EnvironmentHelper.GetEnvironmentVariable("Transport_UseSpecific");

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

            if (!(Activator.CreateInstance(configurerType) is IConfigureTransportInfrastructure configurer))
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
        }

        protected async Task StartPump(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, TransportTransactionMode transactionMode, Action<string, Exception> onCriticalError = null)
        {
            InputQueueName = GetTestName() + transactionMode;
            ErrorQueueName = $"{InputQueueName}.error";

#pragma warning disable SA1512
            //var endpointConfiguration = new EndpointConfiguration(InputQueueName);
            //endpointConfiguration.SendFailedMessagesTo(ErrorQueueName);

            // Single-line comments should not be followed by blank line
            //transportSettings = endpointConfiguration.GetSettings();
#pragma warning restore SA1512 // Single-line comments should not be followed by blank line

            var queueBindings = new QueueBindings();
            queueBindings.BindReceiving(InputQueueName);
            queueBindings.BindSending(ErrorQueueName);

            Configurer = CreateConfigurer();

            var settings = new Settings(InputQueueName, InputQueueName,
                new StartupDiagnosticEntries(), onCriticalError, true);

            var configuration = Configurer.Configure(settings);

            TransportDefinition = configuration.TransportDefinition;
            TransportInfrastructure = await configuration.TransportDefinition.Initialize(
                settings, new[] 
                {
                    new ReceiveSettings()
                    {
                        Id = "MainPump",
                        ErrorQueueAddress = ErrorQueueName,
                        LocalAddress = InputQueueName,
                        settings = new PushSettings(InputQueueName, ErrorQueueName, configuration.PurgeInputQueueOnStartup, transactionMode),
                        UsePublishSubscribe = true
                    }
                }).ConfigureAwait(false);

            IgnoreUnsupportedTransactionModes(transactionMode);

            MessagePump = TransportInfrastructure.Receivers[0];

            MessagePump.Start(configuration.PushRuntimeSettings,
                context =>
                {
                    if (context.Headers.ContainsKey(TestIdHeaderName) && context.Headers[TestIdHeaderName] == testId)
                    {
                        return onMessage(context);
                    }

                    return Task.FromResult(0);
                }, context =>
                {
                    if (context.Message.Headers.ContainsKey(TestIdHeaderName) &&
                        context.Message.Headers[TestIdHeaderName] == testId)
                    {
                        return onError(context);
                    }

                    return Task.FromResult(ErrorHandleResult.Handled);
                });
        }

        string GetUserName()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return $"{Environment.UserDomainName}\\{Environment.UserName}";
            }

            return Environment.UserName;
        }

        void IgnoreUnsupportedTransactionModes(TransportTransactionMode requestedTransactionMode)
        {
            if (TransportDefinition.MaxSupportedTransactionMode < requestedTransactionMode)
            {
                Assert.Ignore($"Only relevant for transports supporting {requestedTransactionMode} or higher");
            }
        }

        protected Task SendMessage(string address,
            Dictionary<string, string> headers = null,
            TransportTransaction transportTransaction = null,
            Dictionary<string, string> messageProperties = null,
            DispatchConsistency dispatchConsistency = DispatchConsistency.Default)
        {
            var messageId = Guid.NewGuid().ToString();
            var message = new OutgoingMessage(messageId, headers ?? new Dictionary<string, string>(), new byte[0]);

            if (message.Headers.ContainsKey(TestIdHeaderName) == false)
            {
                message.Headers.Add(TestIdHeaderName, testId);
            }

            var dispatcher = TransportInfrastructure.Dispatcher;

            if (transportTransaction == null)
            {
                transportTransaction = new TransportTransaction();
            }

            var transportOperation = new TransportOperation(message, new UnicastAddressTag(address), messageProperties ?? new Dictionary<string, string>(), dispatchConsistency);

            return dispatcher.Dispatch(new TransportOperations(transportOperation), transportTransaction);
        }

        protected void OnTestTimeout(Action onTimeoutAction)
        {
            testCancellationTokenSource = Debugger.IsAttached ? new CancellationTokenSource() : new CancellationTokenSource(TimeSpan.FromSeconds(30));

            testCancellationTokenSource.Token.Register(onTimeoutAction);
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
        protected static TransportTestLoggerFactory LogFactory;

        string testId;

        TransportDefinition TransportDefinition;
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