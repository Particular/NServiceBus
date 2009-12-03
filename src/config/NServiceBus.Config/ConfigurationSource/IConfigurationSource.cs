namespace NServiceBus.Config.ConfigurationSource
{
    /// <summary>
    /// Abstraction of a source of configuration data.
    /// </summary>
    public interface IConfigurationSource
    {
        /// <summary>
        /// Returns configuration data based on the given type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T GetConfiguration<T>() where T : class,new();
    }
}