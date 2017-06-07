namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using Faults;
    using Logging;
    using Transports;
    using Unicast;
    using Unicast.Queuing;

    class ManageMessageFailuresWithoutSlr : IManageMessageFailures
    {
        ISendMessages messageSender;
        readonly Configure config;
        readonly Address failedQueue;
        static ILog Logger = LogManager.GetLogger<ManageMessageFailuresWithoutSlr>();

        Address localAddress;
        Address errorQueue;

        public ManageMessageFailuresWithoutSlr(IManageMessageFailures mainFailureManager, ISendMessages messageSender, Configure config, Address failedQueue = null)
        {
            this.messageSender = messageSender;
            this.config = config;
            this.failedQueue = failedQueue;
            var mainTransportFailureManager = mainFailureManager as Faults.Forwarder.FaultManager;
            if (mainTransportFailureManager != null)
            {
                errorQueue = mainTransportFailureManager.ErrorQueue;
            }
        }

        public void SerializationFailedForMessage(TransportMessage message, Exception e)
        {
            SendFailureMessage(message, e, "SerializationFailed");
        }

        public void ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
        {
            SendFailureMessage(message, e, "ProcessingFailed"); 
        }

        void SendFailureMessage(TransportMessage message, Exception e, string reason)
        {
            if (errorQueue == null)
            {
                Logger.Error("Message processing always fails for message with ID " + message.Id + ".", e);
                return;
            }

            message.SetExceptionHeaders(e, failedQueue ?? localAddress ?? config.LocalAddress,reason);

            try
            {
                messageSender.Send(message, new SendOptions(errorQueue));
            }
            catch (QueueNotFoundException exception)
            {
                var errorMessage = string.Format("Could not forward failed message to error queue '{0}' as it could not be found.", exception.Queue);
                Logger.Fatal(errorMessage);
                throw new InvalidOperationException(errorMessage, exception);
            }
            catch (Exception exception)
            {
                var errorMessage = "Could not forward failed message to error queue.";
                Logger.Fatal(errorMessage, exception);
                throw new InvalidOperationException(errorMessage, exception);
            }
        }

        public void Init(Address address)
        {
            localAddress = address;
        }

    }
}