namespace NServiceBus
{
    using Extensibility;

    /// <summary>
    /// Allows the users to control how the publish is performed.
    /// </summary>
    /// <remarks>
    /// The behavior of this class is exposed via extension methods.
    /// </remarks>
    public class PublishOptions : ExtendableOptions
    {
        /// <inheritdoc />
        public PublishOptions()
        {
            Context.GetOrCreate<AttachCorrelationIdBehavior.State>();
        }
    }
}