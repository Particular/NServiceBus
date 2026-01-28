#nullable enable
namespace NServiceBus;

/// <summary>
/// Registration extensions.
/// </summary>
public static class RegistrationExtensions
{
    extension(EndpointConfiguration endpointConfiguration)
    {
        /// <summary>
        /// Access to handler registration.
        /// </summary>
        public HandlerRegistry Handlers => new(endpointConfiguration);
    }
}