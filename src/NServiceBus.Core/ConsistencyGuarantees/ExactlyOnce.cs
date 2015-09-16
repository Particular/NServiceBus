namespace NServiceBus.ConsistencyGuarantees
{
    /// <summary>
    /// Message should be processed exactly once from a business logic perspective.
    /// This can be supported in two ways. 
    /// * Both transport and storage supports distributed transactions.
    /// * Endpoint configured to use the Outbox feature.
    /// </summary>
    public class ExactlyOnce : ConsistencyGuarantee
    {
    }
}