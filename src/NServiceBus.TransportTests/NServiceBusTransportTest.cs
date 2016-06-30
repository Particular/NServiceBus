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
            TransportDefinition = (TransportDefinition) Activator.CreateInstance(transportDefinitionType);
            TransportInfrastructure = TransportDefinition.Initialize(new SettingsHolder(), "");

            ReceiveInfrastructure = TransportInfrastructure.ConfigureReceiveInfrastructure();
            SendInfrastructure = TransportInfrastructure.ConfigureSendInfrastructure();
        }

        protected async Task StartPump(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<bool>> onError, TransportTransactionMode? transactionMode = null)
        {
            var transactionModeToUse = transactionMode ?? GetDefaultTransactionMode();

            if (!transactionModeToUse.HasValue)
            {
                throw new InvalidOperationException("No transaction mode detected");
            }

            IgnoreUnsupportedTransactionModes(transactionModeToUse.Value);
            InputQueueName = GetTestName() + transactionModeToUse.Value;

            MessagePump = ReceiveInfrastructure.MessagePumpFactory();

            var queueBindings = new QueueBindings();
            var errorQueueName = $"{InputQueueName}.error";


            queueBindings.BindReceiving(InputQueueName);
            queueBindings.BindSending(errorQueueName);

            var queueCreator = ReceiveInfrastructure.QueueCreatorFactory();

            await queueCreator.CreateQueueIfNecessary(queueBindings, WindowsIdentity.GetCurrent().Name);

            var pushSettings = new PushSettings(InputQueueName, errorQueueName, true, transactionModeToUse.Value);

            await MessagePump.Init(onMessage, onError, new CriticalError(c => Task.FromResult(0)), pushSettings);

            MessagePump.Start(new PushRuntimeSettings(1));
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

        protected virtual TransportTransactionMode? GetDefaultTransactionMode()
        {
            return null;
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
    }
}