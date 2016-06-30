namespace NServiceBus.TransportTests
{
    public abstract class ScopeTransportTest : NServiceBusTransportTest
    {
        protected override TransportTransactionMode? GetDefaultTransactionMode()
        {
            return TransportTransactionMode.TransactionScope;
        }
    }
}