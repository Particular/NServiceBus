namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using Routing;
    using Settings;
    using Transports;

    public abstract class NServiceBusTransportTest
    {
        string ErrorQueueName => $"{InputQueueName}.error";

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

            MessagePump = ReceiveInfrastructure.MessagePumpFactory();

            var queueBindings = new QueueBindings();


            queueBindings.BindReceiving(InputQueueName);
            queueBindings.BindSending(ErrorQueueName);

            var queueCreator = ReceiveInfrastructure.QueueCreatorFactory();

            await queueCreator.CreateQueueIfNecessary(queueBindings, WindowsIdentity.GetCurrent().Name);

            var pushSettings = new PushSettings(InputQueueName, ErrorQueueName, true, transactionModeToUse.Value);

            await MessagePump.Init(onMessage, onError, new CriticalError(c => Task.FromResult(0)), pushSettings);

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

        protected virtual TransportTransactionMode? GetDefaultTransactionMode()
        {
            return null;
        }

        Type DetectTransportTypeByConvention()
        {
            return typeof(MsmqTransport); //todo
        }

        protected string InputQueueName = "when_scope_dispose_throws";

        TransportReceiveInfrastructure ReceiveInfrastructure;
        TransportSendInfrastructure SendInfrastructure;

        TransportInfrastructure TransportInfrastructure;
        IPushMessages MessagePump;

        TransportDefinition TransportDefinition;
    }
}