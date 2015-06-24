namespace NServiceBus.ConsistencyGuarantees
{
    /// <summary>
    /// Message should be delivered at least once. It's ok to not be atomic with the ongoing receive transaction
    /// should one be present
    /// </summary>
    public class AtLeastOnce : ConsistencyGuarantee
    {
        
    }
}