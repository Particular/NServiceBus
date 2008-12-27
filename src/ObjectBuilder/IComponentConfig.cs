namespace NServiceBus.ObjectBuilder
{
    /// <summary>
    /// Used to configure the values to be set for the various
    /// properties on a component.
    /// </summary>
    public interface IComponentConfig
    {
        /// <summary>
        /// Configures the value of the named property of the component.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        IComponentConfig ConfigureProperty(string name, object value);
    }
}
