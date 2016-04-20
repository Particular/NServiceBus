namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class AddExceptionHeadersBehavior : Behavior<IFaultContext>
    {
        public override Task Invoke(IFaultContext context, Func<Task> next)
        {
            context.Message.SetExceptionHeaders(context.Exception, context.SourceQueueAddress);

            return next();
        }
    }
}
