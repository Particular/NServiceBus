namespace NServiceBus.Config.ConfigurationSource
{
    /// <summary>
    /// Abstraction of a source of configuration data.
    /// Implement this interface if you want to change the source of all configuration data.
    /// If you want to change the source of only a specific set of configuration data,
    /// implement <see cref="IProvideConfiguration&lt;T&gt;" /> instead.
    /// </summary>
    public interface IConfigurationSource
    {
        /// <summary>
        /// Returns configuration data based on the given type.
        /// </summary>
        T GetConfiguration<T>() where T : class, new();
    }

    /// <summary>
    /// Abstraction of a configuration source for a given piece of configuration data.
    /// </summary>
    public interface IProvideConfiguration<T>
    {
        /// <summary>
        /// Returns configuration data for the given type.
        /// </summary>
        T GetConfiguration();
    }
}