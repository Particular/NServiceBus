namespace NServiceBus.ConsistencyGuarantees
{
    /// <summary>
    /// Guarantees that all outgoing operations will be atomic with the current receive operation
    /// </summary>
    public class AtomicWithReceiveOperation : ConsistencyGuarantee
    {

    }
}