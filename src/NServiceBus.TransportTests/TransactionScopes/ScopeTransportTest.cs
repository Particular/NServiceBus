namespace NServiceBus.TransportTests
{
    public class ScopeTransportTest : NServiceBusTransportTest
    {
        protected override TransportTransactionMode RequestedTransactionMode()
        {
            return TransportTransactionMode.TransactionScope;
        }
    }
}