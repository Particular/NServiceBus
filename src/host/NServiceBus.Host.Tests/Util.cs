using NServiceBus.Host.Internal;
using NServiceBus.Host.Internal.ProfileHandlers;

namespace NServiceBus.Host.Tests
{
    public static class Util
    {
        public static Configure Init<T>() where T : IConfigureThisEndpoint, new()
        {
            return new ConfigurationBuilder(new T(), new[] {new ProductionProfileHandler()}).Build();
        }
    }
}
