namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;

    class MessageDrivenSubscribeTerminator : PipelineTerminator<ISubscribeContext>
    {
        protected override Task Terminate(ISubscribeContext context)
        {
            return TaskEx.CompletedTask;
        }
    }
}