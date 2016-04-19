namespace NServiceBus.Recoverability.Faults
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class SetErrorQueueBehavior : Behavior<IFaultContext>
    {
        public SetErrorQueueBehavior(string errorQueueAddress)
        {
            Guard.AgainstNullAndEmpty(nameof(errorQueueAddress), errorQueueAddress);

            this.errorQueueAddress = errorQueueAddress;
        }


        public override Task Invoke(IFaultContext context, Func<Task> next)
        {
            context.ErrorQueueAddress = errorQueueAddress;

            return next();
        }

        readonly string errorQueueAddress;
    }
}