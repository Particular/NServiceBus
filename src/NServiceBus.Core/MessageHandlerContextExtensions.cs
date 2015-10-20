namespace NServiceBus.Extensibility
{
    using System.ComponentModel;

    /// <summary>
    /// Provides extension methods for advanced MessageHandlerContext operations.
    /// </summary>
    public static class MessageHandlerContextExtensions
    {
        /// <summary>
        /// Returns the <see cref="ContextBag"/> of the current context.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static ContextBag GetExtensions(this IMessageHandlerContext handlerContext)
        {
            return ((MessageHandlerContext) handlerContext).Context;
        }
    }
}