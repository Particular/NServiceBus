namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;

    class MessageDrivenUnsubscribeTerminator : PipelineTerminator<IUnsubscribeContext>
    {
        protected override Task Terminate(IUnsubscribeContext context)
        {
            return TaskEx.CompletedTask;
        }
    }
}