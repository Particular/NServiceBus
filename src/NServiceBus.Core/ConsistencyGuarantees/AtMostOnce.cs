namespace NServiceBus.ConsistencyGuarantees
{
    /// <summary>
    /// Message should be handled at most once. This is the lowest guarantee and allows the transport
    ///  to avoid any kind of transactions when performing the receive operation.
    /// </summary>
    public class AtMostOnce : ConsistencyGuarantee
    {   
    }
}