namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.Pipeline.Contexts;

    class MessageHandlerContext : MessageProcessingContext, IMessageHandlerContext
    {
        public MessageHandlerContext(InvokeHandlerContext context)
            : base(context)
        {
            this.context = context;
        }

        public Task HandleCurrentMessageLaterAsync()
        {
            return bus.HandleCurrentMessageLaterAsync(context);
        }

        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            context.DoNotInvokeAnyMoreHandlers();
        }

        InvokeHandlerContext context;
    }
}