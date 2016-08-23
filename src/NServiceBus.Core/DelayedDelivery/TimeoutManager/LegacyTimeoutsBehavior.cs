namespace NServiceBus.DelayedDelivery.TimeoutManager
{
    using System.Threading.Tasks;
    using Transport;

    class LegacyTimeoutsBehavior
    {
        public Task Invoke(MessageContext pushContext)
        {
            throw new System.NotImplementedException();
        }
    }
}