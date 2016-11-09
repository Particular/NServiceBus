namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;

    class NonNativePublishSubscribeTerminator : PipelineTerminator<ISubscribeContext>
    {
        IUnicastPublishSubscribe publishSubscribe;

        public NonNativePublishSubscribeTerminator(IUnicastPublishSubscribe publishSubscribe)
        {
            this.publishSubscribe = publishSubscribe;
        }

        protected override Task Terminate(ISubscribeContext context)
        {
            return publishSubscribe.Subscribe(context);
        }
    }
}