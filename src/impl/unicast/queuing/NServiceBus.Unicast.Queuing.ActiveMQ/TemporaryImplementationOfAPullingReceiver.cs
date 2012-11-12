namespace NServiceBus.Unicast.Queuing.ActiveMQ
{
    using System.Threading;

    using NServiceBus.Unicast.Transport;

    /// <summary>
    /// Just temporary implementation.
    /// </summary>
    public class TemporaryImplementationOfAPullingReceiver : IReceiveMessages
    {
        private readonly INotifyMessageReceived notifyMessageReceived;
        private readonly AutoResetEvent messageReceived = new AutoResetEvent(false);
        private readonly AutoResetEvent messageConsumed = new AutoResetEvent(true);
        private TransportMessageReceivedEventArgs lastEvent;

        public TemporaryImplementationOfAPullingReceiver(INotifyMessageReceived notifyMessageReceived)
        {
            this.notifyMessageReceived = notifyMessageReceived;
            this.notifyMessageReceived.MessageReceived += this.OnMessageReceived;
        }

        private void OnMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            this.messageConsumed.WaitOne();
            this.lastEvent = e;
            this.messageReceived.Set();
        }

        public void Init(Address address, bool transactional)
        {
            this.notifyMessageReceived.Init(address, transactional);
        }

        public bool HasMessage()
        {
            return this.lastEvent != null;
        }

        public TransportMessage Receive()
        {
            this.messageReceived.WaitOne();
            TransportMessage t = this.lastEvent.Message;
            this.lastEvent = null;
            this.messageConsumed.Set();

            return t;
        }
    }
}