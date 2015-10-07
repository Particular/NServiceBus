namespace NServiceBus.Unicast
{
    using Pipeline;

    internal partial class ContextualBusInterface : IBusInterface
    {
        public ContextualBusInterface(BehaviorContextStacker contextStacker, BusOperations busOperations)
        {
            this.contextStacker = contextStacker;
            this.busOperations = busOperations;
        }
        
        public IBusContext CreateSendContext()
        {
            return new BusContext(incomingContext, busOperations);
        }
        
        BehaviorContext incomingContext => contextStacker.GetCurrentOrRootContext();
        BehaviorContextStacker contextStacker;
        BusOperations busOperations;
    }
}