namespace NServiceBus.Recoverability.Faults
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class AddExceptionHeadersBehavior : Behavior<IFaultContext>
    {
        public AddExceptionHeadersBehavior(string localAddress)
        {
            Guard.AgainstNullAndEmpty(nameof(localAddress), localAddress);

            this.localAddress = localAddress;
        }

        public override Task Invoke(IFaultContext context, Func<Task> next)
        {
            var headers = context.Message.Headers;
            ExceptionHeaderHelper.SetExceptionHeaders(headers, context.Exception, localAddress);

            headers.Remove(Headers.Retries);
            headers.Remove(Headers.FLRetries);

            return next();
        }

        readonly string localAddress;
    }
}
