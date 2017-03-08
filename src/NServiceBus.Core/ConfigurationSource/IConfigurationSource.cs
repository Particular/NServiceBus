namespace NServiceBus.Config.ConfigurationSource
{
    /// <summary>
    /// Abstraction of a source of configuration data.
    /// Implement this interface if you want to change the source of all configuration data.
    /// If you want to change the source of only a specific set of configuration data,
    /// implement <see cref="IProvideConfiguration&lt;T&gt;" /> instead.
    /// </summary>
    [ObsoleteEx(Message = "The use of the IConfigurationSource is discouraged. Code configuration is prefered over configuration sources.",
        RemoveInVersion ="8.0",
        TreatAsErrorFromVersion = "7.0")]
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
    [ObsoleteEx(Message = "The use of the IProvideConfiguration is discouraged. Code configuration is prefered over configuration sources.",
        RemoveInVersion = "8.0",
        TreatAsErrorFromVersion = "7.0")]
    public interface IProvideConfiguration<T>
    {
        /// <summary>
        /// Returns configuration data for the given type.
        /// </summary>
        T GetConfiguration();
    }
}