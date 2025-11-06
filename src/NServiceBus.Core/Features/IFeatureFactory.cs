#nullable enable
namespace NServiceBus.Features;

/// <summary>
///
/// </summary>
public interface IFeatureFactory
{
    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    static abstract Feature Create();
}