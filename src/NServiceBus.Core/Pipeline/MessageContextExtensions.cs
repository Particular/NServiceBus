namespace NServiceBus.Pipeline
{
    using System.Threading.Tasks;
    using Extensibility;
    using ObjectBuilder;
    using Transport;

    /// <summary>
    /// Raw dispatch
    /// </summary>
    public static class RawDispatchExtensions
    {
        // If this would be on IExtensible we could allow raw dispatches elsewhere
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="operations"></param>
        /// <returns></returns>
        public static Task Dispatch(this MessageContext context, params TransportOperation[] operations)
        {
            var cache = context.Extensions.Get<IPipelineCache>();
            var pipeline = cache.Pipeline<IDispatchContext>();
            return pipeline.Invoke(new DispatchContext(operations, new ContextToGetItWorking(context)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="operations"></param>
        /// <returns></returns>
        public static Task Dispatch(this ErrorContext context, params TransportOperation[] operations)
        {
            var cache = context.Extensions.Get<IPipelineCache>();
            var pipeline = cache.Pipeline<IDispatchContext>();
            return pipeline.Invoke(new DispatchContext(operations, new ContextToGetItWorking(context)));
        }

        class ContextToGetItWorking : ContextBag, IBehaviorContext
        {
            public ContextToGetItWorking(IExtendable extendable) : base(extendable.Extensions)
            {

            }

            public ContextBag Extensions => this;
            public IBuilder Builder => Get<IBuilder>();
        }
    }


}