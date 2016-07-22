namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using Routing;
    using Settings;
    using Transport;

    public abstract class NServiceBusTransportTest
    {
        public static string SpecificTransport
        {
            get
            {
                var specificTransport = EnvironmentHelper.GetEnvironmentVariable("Transport.UseSpecific");

                return !string.IsNullOrEmpty(specificTransport) ? specificTransport : MsmqDescriptorKey;
            }
        }

        [SetUp]
        public void Setup()
        {
            Configurer = CreateConfigurer();

            transportSettings = new SettingsHolder();
            TransportInfrastructure = Configurer.Configure(transportSettings);

            ReceiveInfrastructure = TransportInfrastructure.ConfigureReceiveInfrastructure();
            SendInfrastructure = TransportInfrastructure.ConfigureSendInfrastructure();

            lazyDispatcher = new Lazy<IDispatchMessages>(() => SendInfrastructure.DispatcherFactory());
        }

        static IConfigureTransportInfrastructure CreateConfigurer()
        {
            var typeName = "Configure" + SpecificTransport + "Infrastructure";

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
        }

        protected async Task StartPump(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, TransportTransactionMode transactionMode, Action<string, Exception> onCriticalError = null)
        {
            IgnoreUnsupportedTransactionModes(transactionMode);

            InputQueueName = GetTestName() + transactionMode;

            MessagePump = ReceiveInfrastructure.MessagePumpFactory();

            var queueBindings = new QueueBindings();
            ErrorQueueName = $"{InputQueueName}.error";

            queueBindings.BindReceiving(InputQueueName);
            queueBindings.BindSending(ErrorQueueName);

            var queueCreator = ReceiveInfrastructure.QueueCreatorFactory();

            await queueCreator.CreateQueueIfNecessary(queueBindings, WindowsIdentity.GetCurrent().Name);

            transportSettings.Set<QueueBindings>(queueBindings);

            var pushSettings = new PushSettings(InputQueueName, ErrorQueueName, true, transactionMode);

            await MessagePump.Init(onMessage, onError, new FakeCriticalError(onCriticalError), pushSettings);

            MessagePump.Start(PushRuntimeSettings.Default);
        }

        void IgnoreUnsupportedTransactionModes(TransportTransactionMode requestedTransactionMode)
        {
            if (TransportInfrastructure.TransactionMode < requestedTransactionMode)
            {
                Assert.Ignore($"Only relevant for transports supporting {requestedTransactionMode} or higher");
            }
        }

        protected Task SendMessage(string address, Dictionary<string, string> headers = null, TransportTransaction transportTransaction = null)
        {
            var messageId = Guid.NewGuid().ToString();
            var message = new OutgoingMessage(messageId, headers ?? new Dictionary<string, string>(), new byte[0]);

            var dispatcher = lazyDispatcher.Value;

            var context = new ContextBag();

            //until we fix the seam we have to do this
            if (transportTransaction != null)
            {
                context.Set(transportTransaction);
            }

            return dispatcher.Dispatch(new TransportOperations(new TransportOperation(message, new UnicastAddressTag(address))), context);
        }

        protected void OnTestTimeout(Action onTimeoutAction)
        {
            testCancellationTokenSource = new CancellationTokenSource();

            testCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));
            testCancellationTokenSource.Token.Register(onTimeoutAction);
        }

        protected virtual TransportInfrastructure CreateTransportInfrastructure()
        {
            var msmqTransportDefinition = new MsmqTransport();
            return msmqTransportDefinition.Initialize(new SettingsHolder(), "");
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

        SettingsHolder transportSettings;
        Lazy<IDispatchMessages> lazyDispatcher;
        TransportReceiveInfrastructure ReceiveInfrastructure;
        TransportSendInfrastructure SendInfrastructure;
        TransportInfrastructure TransportInfrastructure;
        IPushMessages MessagePump;
        CancellationTokenSource testCancellationTokenSource;
        IConfigureTransportInfrastructure Configurer;

        static string MsmqDescriptorKey = "MsmqTransport";

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