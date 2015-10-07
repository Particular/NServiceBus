namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast;

    class MessageHandlerContext : MessageProcessingContext, IMessageHandlerContext
    {
        public MessageHandlerContext(InvokeHandlerContext context, BusOperations busOperations)
            : base(context, busOperations)
        {
            this.context = context;
        }

        public Task HandleCurrentMessageLaterAsync()
        {
            return BusOperations.HandleCurrentMessageLaterAsync(context);
        }

        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            context.DoNotInvokeAnyMoreHandlers();
        }

        InvokeHandlerContext context;
    }
}