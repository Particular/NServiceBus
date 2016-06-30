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
        [SetUp]
        public void Setup()
        {
            var transportDefinitionType = DetectTransportTypeByConvention();
            TransportDefinition = (TransportDefinition) Activator.CreateInstance(transportDefinitionType);
            TransportInfrastructure = TransportDefinition.Initialize(new SettingsHolder(), "");

            OnlyAppliesToTransportsSupporting(RequestedTransactionMode());

            ReceiveInfrastructure = TransportInfrastructure.ConfigureReceiveInfrastructure();
            SendInfrastructure = TransportInfrastructure.ConfigureSendInfrastructure();
        }

        protected async Task StartPump(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<bool>> onError)
        {
            MessagePump = ReceiveInfrastructure.MessagePumpFactory();

            var queueBindings = new QueueBindings();


            queueBindings.BindReceiving(InputQueueName);
            queueBindings.BindSending(ErrorQueueName);

            var queueCreator = ReceiveInfrastructure.QueueCreatorFactory();

            await queueCreator.CreateQueueIfNecessary(queueBindings, WindowsIdentity.GetCurrent().Name);

            var pushSettings = new PushSettings(InputQueueName, ErrorQueueName, true, RequestedTransactionMode());

            await MessagePump.Init(onMessage, onError, new CriticalError(c => Task.FromResult(0)), pushSettings);

            MessagePump.Start(PushRuntimeSettings.Default);
        }


        void OnlyAppliesToTransportsSupporting(TransportTransactionMode requestedTransactionMode)
        {
            if (TransportInfrastructure.TransactionMode < requestedTransactionMode)
            {
                Assert.Ignore($"Only relevant for transports supporting {requestedTransactionMode} or higher");
            }
        }

        protected Task SendMessage(string address)
        {
            var messageId = Guid.NewGuid().ToString();
            var message = new OutgoingMessage(messageId, new Dictionary<string, string>(), new byte[0]);

            var dispatcher = SendInfrastructure.DispatcherFactory();
            return dispatcher.Dispatch(new TransportOperations(new TransportOperation(message, new UnicastAddressTag(address))), new ContextBag());
        }

        protected TransportReceiveInfrastructure ReceiveInfrastructure;
        protected TransportSendInfrastructure SendInfrastructure;
        protected string InputQueueName = "when_scope_dispose_throws";

        protected string ErrorQueueName => $"{InputQueueName}.error";

        protected TransportInfrastructure TransportInfrastructure;
        protected IPushMessages MessagePump;

        protected abstract TransportTransactionMode RequestedTransactionMode();

        Type DetectTransportTypeByConvention()
        {
            return typeof(MsmqTransport);//todo
        }

        TransportDefinition TransportDefinition;
    }
}