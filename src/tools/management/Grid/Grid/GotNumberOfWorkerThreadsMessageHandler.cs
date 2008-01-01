using NServiceBus;
using NServiceBus.Unicast.Transport.Msmq;

namespace Grid
{
    public class GotNumberOfWorkerThreadsMessageHandler : BaseMessageHandler<GotNumberOfWorkerThreadsMessage>
    {
        #region IMessageHandler<GotNumberOfWorkerThreadsMessage> Members

        public override void Handle(GotNumberOfWorkerThreadsMessage message)
        {
            Manager.UpdateNumberOfWorkerThreads(
                this.Bus.SourceOfMessageBeingHandled,
                message.NumberOfWorkerThreads
                );
        }

        #endregion
    }
}
