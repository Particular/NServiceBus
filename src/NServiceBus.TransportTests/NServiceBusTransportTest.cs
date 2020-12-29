using NServiceBus.Transports;
using NServiceBus.Unicast.Messages;

namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using NUnit.Framework;
    using Routing;
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
            TransportInfrastructure = null;
            Configurer = null;
            testCancellationTokenSource = null;
            receiver = null;
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
            receiver?.StopReceive().GetAwaiter().GetResult();
            TransportInfrastructure?.DisposeAsync().GetAwaiter().GetResult();
            Configurer?.Cleanup().GetAwaiter().GetResult();
        }

        protected async Task StartPump(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, TransportTransactionMode transactionMode, Action<string, Exception> onCriticalError = null)
        {
            InputQueueName = GetTestName() + transactionMode;
            ErrorQueueName = $"{InputQueueName}.error";

            Configurer = CreateConfigurer();

            var hostSettings = new HostSettings(InputQueueName,
                string.Empty,
                new StartupDiagnosticEntries(),
                onCriticalError,
                true);

            var configuration = await Configurer.Configure(hostSettings, InputQueueName, ErrorQueueName, transactionMode);

            TransportInfrastructure = configuration.TransportInfrastructure;

            IgnoreUnsupportedTransactionModes(configuration.TransportDefinition, transactionMode);
            IgnoreUnsupportedDeliveryConstraints();

            await TransportInfrastructure.Receivers[0].Initialize(
                new PushRuntimeSettings(8),
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
                    if (context.Message.Headers.ContainsKey(TestIdHeaderName) &&
                        context.Message.Headers[TestIdHeaderName] == testId)
                    {
                        return onError(context);
                    }

                    return Task.FromResult(ErrorHandleResult.Handled);
                }, new MessageMetadata[0], CancellationToken.None);

            await TransportInfrastructure.Receivers[0].StartReceive(CancellationToken.None);

            receiver = TransportInfrastructure.Receivers[0];
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
            //var supportedDeliveryConstraints = TransportInfrastructure.DeliveryConstraints.ToList();
            //var unsupportedDeliveryConstraints = requiredDeliveryConstraints.Where(required => !supportedDeliveryConstraints.Contains(required))
            //    .ToList();

            //if (unsupportedDeliveryConstraints.Any())
            //{
            //    var unsupported = string.Join(",", unsupportedDeliveryConstraints.Select(c => c.Name));
            //    Assert.Ignore($"Transport doesn't support required delivery constraint(s) {unsupported}");
            //}
        }

        void IgnoreUnsupportedTransactionModes(TransportDefinition transportDefinition, TransportTransactionMode requestedTransactionMode)
        {
            if (!transportDefinition.GetSupportedTransactionModes().Contains(requestedTransactionMode))
            {
                Assert.Ignore($"Only relevant for transports supporting {requestedTransactionMode} or higher");
            }
        }

        protected Task SendMessage(string address,
            Dictionary<string, string> headers = null,
            TransportTransaction transportTransaction = null,
            OperationProperties operationProperties = null,
            DispatchConsistency dispatchConsistency = DispatchConsistency.Default)
        {
            var messageId = Guid.NewGuid().ToString();
            var message = new OutgoingMessage(messageId, headers ?? new Dictionary<string, string>(), new byte[0]);

            if (message.Headers.ContainsKey(TestIdHeaderName) == false)
            {
                message.Headers.Add(TestIdHeaderName, testId);
            }

            if (transportTransaction == null)
            {
                transportTransaction = new TransportTransaction();
            }

            var transportOperation = new TransportOperation(message, new UnicastAddressTag(address), operationProperties?.ToDictionary(), dispatchConsistency);

            return TransportInfrastructure.Dispatcher.Dispatch(new TransportOperations(transportOperation), transportTransaction);
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

        List<Type> requiredDeliveryConstraints = new List<Type>();
        TransportInfrastructure TransportInfrastructure;
        CancellationTokenSource testCancellationTokenSource;
        IConfigureTransportInfrastructure Configurer;
        IMessageReceiver receiver;

        const string DefaultTransportDescriptorKey = "LearningTransport";
        const string TestIdHeaderName = "TransportTest.TestId";

        static Lazy<List<Type>> transportDefinitions = new Lazy<List<Type>>(() => TypeScanner.GetAllTypesAssignableTo<TransportDefinition>().ToList());

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