using log4net;
using NServiceBus.Faults.Forwarder;
using NServiceBus.Management.Retries.Helpers;

namespace NServiceBus.Management.Retries
{
    public class SecondLevelRetriesFaultManager : FaultManager
    {
        ILog Logger = LogManager.GetLogger("SecondLevelRetriesFaultManager");

        protected override void Send(Unicast.Transport.TransportMessage message, Address errorQueue)
        {
            var failedQ = TransportMessageHelpers.GetReplyToAddress(message);

            var sat = Configure.Instance.Builder.Build<SecondLevelRetries>();

            if (failedQ == sat.InputAddress)
            {
                Logger.InfoFormat("The message was sent from the SecondLevelRetries satellite. Sending the message direct to the error queue!");

                base.Send(message, sat.ErrorQueue);
                return;
            }

            base.Send(message, errorQueue);
        }
    }
}