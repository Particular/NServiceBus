namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Transport;

    class RawPipelineExecutor : IPipelineExecutor
    {
        public RawPipelineExecutor(IDispatchMessages dispatcher, Func<MessageContext, IDispatchMessages, Task> rawPipeline)
        {
            dispatchMessages = dispatcher;
            this.rawPipeline = rawPipeline;
        }

        public Task Invoke(MessageContext messageContext)
        {
            return rawPipeline(messageContext, dispatchMessages);
        }

        Func<MessageContext, IDispatchMessages, Task> rawPipeline;
        IDispatchMessages dispatchMessages;
    }
}