namespace NServiceBus
{
    using Features;

    /// <summary>
    /// The factory of <see cref="IUnicastPublish"/>.
    /// </summary>
    public interface IUnicastPublishProvider
    {
        /// <summary>
        /// bla.
        /// </summary>
        /// <param name="context"></param>
        IUnicastPublish Get(FeatureConfigurationContext context);
    }
}