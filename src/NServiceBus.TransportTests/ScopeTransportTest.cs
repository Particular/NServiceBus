namespace NServiceBus.TransportTests
{
    public class ScopeTransportTest : NServiceBusTransportTest
    {
        protected override TransportTransactionMode? GetDefaultTransactionMode()
        {
            return TransportTransactionMode.TransactionScope;
        }
    }
}