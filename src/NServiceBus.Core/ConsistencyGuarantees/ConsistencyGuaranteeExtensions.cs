namespace NServiceBus.ConsistencyGuarantees
{
    using NServiceBus.TransportDispatch;

    static class ConsistencyGuaranteeExtensions
    {
        public static ConsistencyGuarantee GetConsistencyGuarantee(this DispatchContext context)
        {
            return context.Get<ConsistencyGuarantee>();
        }
    }
}