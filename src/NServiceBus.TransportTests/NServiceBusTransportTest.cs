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
    using Transports;

    public abstract class NServiceBusTransportTest
    {
        [SetUp]
        public void Setup()
        {
            var transportDefinitionType = DetectTransportTypeByConvention();
            TransportDefinition = (TransportDefinition)Activator.CreateInstance(transportDefinitionType);
            TransportInfrastructure = TransportDefinition.Initialize(new SettingsHolder(), "");

            ReceiveInfrastructure = TransportInfrastructure.ConfigureReceiveInfrastructure();
            SendInfrastructure = TransportInfrastructure.ConfigureSendInfrastructure();
        }

        [TearDown]
        public void TearDown()
        {
            testCancellationTokenSource?.Dispose();
        }

        protected async Task StartPump(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<bool>> onError, TransportTransactionMode transactionMode, Func<string, Exception, Task> onCriticalError = null)
        {
            IgnoreUnsupportedTransactionModes(transactionMode);

            InputQueueName = GetTestName() + transactionMode;

            MessagePump = ReceiveInfrastructure.MessagePumpFactory();

            var queueBindings = new QueueBindings();
            var errorQueueName = $"{InputQueueName}.error";


            queueBindings.BindReceiving(InputQueueName);
            queueBindings.BindSending(errorQueueName);

            var queueCreator = ReceiveInfrastructure.QueueCreatorFactory();

            await queueCreator.CreateQueueIfNecessary(queueBindings, WindowsIdentity.GetCurrent().Name);

            var pushSettings = new PushSettings(InputQueueName, errorQueueName, true, transactionMode);

            await MessagePump.Init(onMessage, onError, onCriticalError, pushSettings);

            MessagePump.Start(PushRuntimeSettings.Default);
        }


        void IgnoreUnsupportedTransactionModes(TransportTransactionMode requestedTransactionMode)
        {
            if (TransportInfrastructure.TransactionMode < requestedTransactionMode)
            {
                Assert.Ignore($"Only relevant for transports supporting {requestedTransactionMode} or higher");
            }
        }

        protected Task SendMessage(string address, Dictionary<string, string> headers = null)
        {
            var messageId = Guid.NewGuid().ToString();
            var message = new OutgoingMessage(messageId, headers ?? new Dictionary<string, string>(), new byte[0]);

            var dispatcher = SendInfrastructure.DispatcherFactory();
            return dispatcher.Dispatch(new TransportOperations(new TransportOperation(message, new UnicastAddressTag(address))), new ContextBag());
        }

        protected void OnTestTimeout(Action onTimeoutAction)
        {
            testCancellationTokenSource = new CancellationTokenSource();

            testCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));
            testCancellationTokenSource.Token.Register(onTimeoutAction);

        }

        Type DetectTransportTypeByConvention()
        {
            return typeof(MsmqTransport); //todo
        }

        string GetTestName()
        {
            var index = 1;
            var frame = new StackFrame(index);
            Type type;

            while (true)
            {
                type = frame.GetMethod().DeclaringType;
                if (!type.IsAbstract && typeof(NServiceBusTransportTest).IsAssignableFrom(type))
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

        TransportReceiveInfrastructure ReceiveInfrastructure;
        TransportSendInfrastructure SendInfrastructure;
        TransportInfrastructure TransportInfrastructure;
        IPushMessages MessagePump;
        TransportDefinition TransportDefinition;
        CancellationTokenSource testCancellationTokenSource;
    }
}