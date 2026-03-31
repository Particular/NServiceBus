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
        /// Registry for generated handler registration extensions.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When message handlers are decorated with the <see cref="HandlerAttribute" /> and sagas are decorated with the
        /// <see cref="SagaAttribute" />, methods to register these handlers and sagas are generated here.
        /// </para>
        /// <para>
        /// For more information, see the remarks for <see cref="HandlerAttribute" /> and <see cref="SagaAttribute" />.
        /// </para>
        /// </remarks>
        public HandlerRegistry Handlers => new(endpointConfiguration);
    }
}