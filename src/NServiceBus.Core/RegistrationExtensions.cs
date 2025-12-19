#nullable enable
namespace NServiceBus;

/// <summary>
///
/// </summary>
public static class RegistrationExtensions
{
    extension(EndpointConfiguration endpointConfiguration)
    {
        /// <summary>
        ///
        /// </summary>
        public HandlersRegistry Handlers => new(endpointConfiguration);
    }
}