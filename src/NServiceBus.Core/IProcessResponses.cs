namespace NServiceBus
{
    using JetBrains.Annotations;

    /// <summary>
    /// Defines a response handler.
    /// </summary>
    /// <typeparam name="T">The type of response to be handled.</typeparam>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface IProcessResponses<T>
    {
        /// <summary>
        /// Handles a response.
        /// </summary>
        /// <param name="message">The response to handle.</param>
        /// <param name="context">The response context</param>
        /// <remarks>
        /// This method will be called when a response arrives on the bus and should contain
        /// the custom logic to execute when the response is received.</remarks>
        void Handle(T message, IResponseContext context);
    }
}