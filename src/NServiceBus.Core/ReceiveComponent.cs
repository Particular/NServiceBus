namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Settings;
    using Transport;

    class ReceiveComponent
    {
        public ReceiveComponent(bool isSendOnlyEndpoint, TransportInfrastructure transportInfrastructure)
        {
            this.isSendOnlyEndpoint = isSendOnlyEndpoint;
            this.transportInfrastructure = transportInfrastructure;
        }


        public void Initialize(ReadOnlySettings settings)
        {
            this.settings = settings;

            if (isSendOnlyEndpoint)
            {
                return;
            }

            receiveInfrastructure = transportInfrastructure.ConfigureReceiveInfrastructure();
        }

        public IPushMessages BuildMessagePump()
        {
            return receiveInfrastructure.MessagePumpFactory();
        }

        public Task CreateQueuesIfNecessary(string username)
        {
            if (isSendOnlyEndpoint)
            {
                return TaskEx.CompletedTask;
            }

            if (!settings.CreateQueues())
            {
                return TaskEx.CompletedTask;
            }

            var queueCreator = receiveInfrastructure.QueueCreatorFactory();
            var queueBindings = settings.Get<QueueBindings>();

            return queueCreator.CreateQueueIfNecessary(queueBindings, username);
        }

        public async Task PerformPreStartupChecks()
        {
            if (isSendOnlyEndpoint)
            {
                return;
            }

            var result = await receiveInfrastructure.PreStartupCheck().ConfigureAwait(false);

            if (!result.Succeeded)
            {
                throw new Exception($"Pre start-up check failed: {result.ErrorMessage}");
            }
        }

        TransportReceiveInfrastructure receiveInfrastructure;
        TransportInfrastructure transportInfrastructure;
        bool isSendOnlyEndpoint;
        ReadOnlySettings settings;
    }
}