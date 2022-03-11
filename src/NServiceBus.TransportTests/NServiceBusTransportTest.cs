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
            transportInfrastructure = null;
            configurer = null;
            testCancellationTokenSource = null;
            receiver = null;
        }
        protected static IConfigureTransportInfrastructure CreateConfigurer()
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
                throw new InvalidOperationException($"Transport Test project must include a non-namespaced class named '{typeName}' implementing {nameof(IConfigureTransportInfrastructure)}.");
            }

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0078 // Use pattern matching (may change code meaning)
#pragma warning disable IDE0083 // Use pattern matching
            if (!(Activator.CreateInstance(configurerType) is IConfigureTransportInfrastructure configurer))
#pragma warning restore IDE0083 // Use pattern matching
#pragma warning restore IDE0078 // Use pattern matching (may change code meaning)
#pragma warning restore IDE0079 // Remove unnecessary suppression
            {
                throw new InvalidOperationException($"{typeName} does not implement {nameof(IConfigureTransportInfrastructure)}.");
            }

            return configurer;
        }

        [TearDown]
        public async Task TearDown()
        {
            await StopPump();
            await (transportInfrastructure != null ? transportInfrastructure.Shutdown() : Task.CompletedTask);
            await (configurer != null ? configurer.Cleanup() : Task.CompletedTask);
            testCancellationTokenSource?.Dispose();
        }

        protected async Task StartPump(OnMessage onMessage, OnError onError, TransportTransactionMode transactionMode, Action<string, Exception, CancellationToken> onCriticalError = null, CancellationToken cancellationToken = default)
        {
            onMessage = onMessage ?? throw new ArgumentNullException(nameof(onMessage));
            onError = onError ?? throw new ArgumentNullException(nameof(onError));

            InputQueueName = GetTestName() + transactionMode;
            ErrorQueueName = $"{InputQueueName}.error";

            configurer = CreateConfigurer();

            var hostSettings = new HostSettings(
                InputQueueName,
                string.Empty,
                new StartupDiagnosticEntries(),
                (message, ex, token) =>
                {
                    if (onCriticalError == null)
                    {
                        testCancellationTokenSource.Cancel();
                        Assert.Fail($"{message}{Environment.NewLine}{ex}");
                    }

                    onCriticalError(message, ex, token);
                },
                true);

            var transport = configurer.CreateTransportDefinition();

            IgnoreUnsupportedTransactionModes(transport, transactionMode);
            transport.TransportTransactionMode = transactionMode;

            transportInfrastructure = await configurer.Configure(transport, hostSettings, new QueueAddress(InputQueueName), ErrorQueueName, cancellationToken);

            receiver = transportInfrastructure.Receivers.Single().Value;

            await receiver.Initialize(
                new PushRuntimeSettings(8),
                (context, token) =>
                    context.Headers.Contains(TestIdHeaderName, testId) ? onMessage(context, token) : Task.CompletedTask,
                (context, token) =>
                    context.Message.Headers.Contains(TestIdHeaderName, testId) ? onError(context, token) : Task.FromResult(ErrorHandleResult.Handled),
                cancellationToken);

            await receiver.StartReceive(cancellationToken);
        }

        protected async Task StopPump(CancellationToken cancellationToken = default)
        {
            if (receiver == null)
            {
                return;
            }

            await receiver.StopReceive(cancellationToken);

            receiver = null;
        }

        string GetUserName()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return $"{Environment.UserDomainName}\\{Environment.UserName}";
            }

            return Environment.UserName;
        }

        void IgnoreUnsupportedTransactionModes(TransportDefinition transportDefinition, TransportTransactionMode requestedTransactionMode)
        {
            if (!transportDefinition.GetSupportedTransactionModes().Contains(requestedTransactionMode))
            {
                Assert.Ignore($"Only relevant for transports supporting {requestedTransactionMode} or higher");
            }
        }

        protected Task SendMessage(
            string address,
            Dictionary<string, string> headers = null,
            TransportTransaction transportTransaction = null,
            DispatchProperties dispatchProperties = null,
            DispatchConsistency dispatchConsistency = DispatchConsistency.Default,
            byte[] body = null,
            CancellationToken cancellationToken = default)
        {
            var messageId = Guid.NewGuid().ToString();
            var message = new OutgoingMessage(messageId, headers ?? new Dictionary<string, string>(), body ?? Array.Empty<byte>());

            if (message.Headers.ContainsKey(TestIdHeaderName) == false)
            {
                message.Headers.Add(TestIdHeaderName, testId);
            }

            if (transportTransaction == null)
            {
                transportTransaction = new TransportTransaction();
            }

            var transportOperation = new TransportOperation(message, new UnicastAddressTag(address), dispatchProperties, dispatchConsistency);

            return transportInfrastructure.Dispatcher.Dispatch(new TransportOperations(transportOperation), transportTransaction, cancellationToken);
        }

        protected void OnTestTimeout(Action onTimeoutAction)
        {
            testCancellationTokenSource = Debugger.IsAttached ? new CancellationTokenSource() : new CancellationTokenSource(TestTimeout);

            testCancellationTokenSource.Token.Register(onTimeoutAction);
        }

        protected static TaskCompletionSource<TResult> CreateTaskCompletionSource<TResult>()
        {
            var source = new TaskCompletionSource<TResult>();

            if (!Debugger.IsAttached)
            {
                _ = new CancellationTokenSource(TestTimeout).Token.Register(() => _ = source.TrySetException(new Exception("The test timed out.")));
            }

            return source;
        }

        protected static TaskCompletionSource CreateTaskCompletionSource()
        {
            var source = new TaskCompletionSource();

            if (!Debugger.IsAttached)
            {
                _ = new CancellationTokenSource(TestTimeout).Token.Register(() => _ = source.TrySetException(new Exception("The test timed out.")));
            }

            return source;
        }

        protected static string GetTestName()
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
        protected static TimeSpan TestTimeout = TimeSpan.FromSeconds(30);

        string testId;

        TransportInfrastructure transportInfrastructure;
        CancellationTokenSource testCancellationTokenSource;
        IConfigureTransportInfrastructure configurer;
        protected IMessageReceiver receiver;

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
