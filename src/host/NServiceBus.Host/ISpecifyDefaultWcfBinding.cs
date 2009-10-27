using System.ServiceModel.Channels;

namespace NServiceBus.Host
{
    /// <summary>
    /// Allows the user to specify the default binding for service endpoints
    /// </summary>
    public interface ISpecifyDefaultWcfBinding
    {
        /// <summary>
        /// Returns the default binding
        /// </summary>
        /// <returns></returns>
        Binding SpecifyBinding();
    }
}