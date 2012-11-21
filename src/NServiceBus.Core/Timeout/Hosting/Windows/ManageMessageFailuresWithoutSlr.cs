namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using Faults;
    using Logging;
    using Unicast.Queuing;
    using Unicast.Transport;

    internal class ManageMessageFailuresWithoutSlr : IManageMessageFailures
    {
        static readonly ILog Logger = LogManager.GetLogger("IManageMessageFailuresWithoutSLR");

        private Address localAddress;
        private readonly Address errorQueue;

        public ManageMessageFailuresWithoutSlr(IManageMessageFailures mainFailureManager)
        {
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
            var id = message.Id;
            SendFailureMessage(message, e, "ProcessingFailed"); //overwrites message.Id
            message.Id = id;
        }

        void SendFailureMessage(TransportMessage message, Exception e, string reason)
        {
            if (errorQueue == null)
            {
                Logger.Error("Message processing always fails for message with ID " + message.IdForCorrelation + ".", e);
                return;
            }

            SetExceptionHeaders(message, e, reason);
            try
            {
                var sender = Configure.Instance.Builder.Build<ISendMessages>();

                sender.Send(message, errorQueue);
            }
            catch (Exception exception)
            {
                var qnfEx = exception as QueueNotFoundException;
                var errorMessage = qnfEx != null
                                          ? string.Format(
                                              "Could not forward failed message to error queue '{0}' as it could not be found.",
                                              qnfEx.Queue)
                                          : string.Format(
                                              "Could not forward failed message to error queue, reason: {0}.", exception);
                Logger.Fatal(errorMessage);
                throw new InvalidOperationException(errorMessage, exception);
            }
        }

        public void Init(Address address)
        {
            localAddress = address;
        }

        void SetExceptionHeaders(TransportMessage message, Exception e, string reason)
        {
            message.Headers["NServiceBus.ExceptionInfo.Reason"] = reason;
            message.Headers["NServiceBus.ExceptionInfo.ExceptionType"] = e.GetType().FullName;

            if (e.InnerException != null)
                message.Headers["NServiceBus.ExceptionInfo.InnerExceptionType"] = e.InnerException.GetType().FullName;

            message.Headers["NServiceBus.ExceptionInfo.HelpLink"] = e.HelpLink;
            message.Headers["NServiceBus.ExceptionInfo.Message"] = e.Message;
            message.Headers["NServiceBus.ExceptionInfo.Source"] = e.Source;
            message.Headers["NServiceBus.ExceptionInfo.StackTrace"] = e.StackTrace;

            message.Headers[TransportHeaderKeys.OriginalId] = message.Id;

            var failedQ = localAddress ?? Address.Local;

            message.Headers[FaultsHeaderKeys.FailedQ] = failedQ.ToString();
            message.Headers["NServiceBus.TimeOfFailure"] = DateTime.UtcNow.ToWireFormattedString();
        }
    }
}