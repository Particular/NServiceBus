namespace NServiceBus.Routing
{
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// Exposes ways to get routing info on the various pipeline context
    /// </summary>
    public static class RoutingPipelineContextExtensions
    {
        /// <summary>
        /// Tells if this operation is a reply
        /// </summary>
        /// <param name="context">Context being extended</param>
        /// <returns>True if the operation is a reply</returns>
        public static bool IsReply(this OutgoingContext context)
        {
            return context.Get<ExtendableOptions>() is ReplyOptions;
        }

        /// <summary>
        /// Tells if this operation is a publish
        /// </summary>
        /// <param name="context">Context being extended</param>
        /// <returns>True if the operation is a publish</returns>
        public static bool IsPublish(this OutgoingContext context)
        {
            return context.Get<ExtendableOptions>() is PublishOptions;
        }

        /// <summary>
        /// Tells if this operation is a publish
        /// </summary>
        /// <param name="context">Context being extended</param>
        /// <returns>True if the operation is a publish</returns>
        public static bool IsPublish(this PhysicalOutgoingContextStageBehavior.Context context)
        {
            return context.Get<ExtendableOptions>() is PublishOptions;
        }

        /// <summary>
        /// Tells if this operation is a send
        /// </summary>
        /// <param name="context">Context being extended</param>
        /// <returns>True if the operation is a publish</returns>
        public static bool IsSend(this OutgoingContext context)
        {
            return context.Get<ExtendableOptions>() is SendOptions || context.Get<ExtendableOptions>() is SendLocalOptions;
        }
        /// <summary>
        /// Tells if this operation is a reply
        /// </summary>
        /// <param name="context">Context being extended</param>
        /// <returns>True if the operation is a reply</returns>
        public static bool IsReply(this PhysicalOutgoingContextStageBehavior.Context context)
        {
            return context.Get<ExtendableOptions>() is ReplyOptions;
        }
    }
}