namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;
    class NonNativePublishUnsubscribeTerminator : PipelineTerminator<IUnsubscribeContext>
    {
        IUnicastPublishSubscribe publishSubscribe;

        public NonNativePublishUnsubscribeTerminator(IUnicastPublishSubscribe publishSubscribe)
        {
            this.publishSubscribe = publishSubscribe;
        }

        protected override Task Terminate(IUnsubscribeContext context)
        {
            return publishSubscribe.Unsubscribe(context);
        }
    }
}