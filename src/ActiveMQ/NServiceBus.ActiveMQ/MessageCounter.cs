namespace NServiceBus.Transports.ActiveMQ
{
    using System.Threading;

    public class MessageCounter : IMessageCounter
    {
        private int messageCount;

        public void Increment()
        {
            Interlocked.Increment(ref this.messageCount);
        }

        public void Decrement()
        {
            Interlocked.Decrement(ref this.messageCount);
        }

        public bool Wait(int timeout)
        {
            return SpinWait.SpinUntil(() => Interlocked.CompareExchange(ref this.messageCount, 0, 0) == 0, timeout);
        }
    }
}