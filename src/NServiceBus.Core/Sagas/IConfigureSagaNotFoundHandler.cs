#nullable enable
namespace NServiceBus;

/// <summary>
/// Implementation provided by the infrastructure - don't implement this
/// unless you intend
/// to substantially change the way sagas work.
/// </summary>
public interface IConfigureSagaNotFoundHandler
{
    /// <summary>
    /// Specifies the optional saga not found handler for this saga instance.
    /// </summary>
    void ConfigureSagaNotFoundHandler<TNotFoundHandler>() where TNotFoundHandler : IHandleSagaNotFound;
}