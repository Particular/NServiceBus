namespace NServiceBus;

using Extensibility;

/// <summary>
/// Allows the users to control how the publish is performed.
/// </summary>
/// <remarks>
/// The behavior of this class is exposed via extension methods.
/// </remarks>
public class PublishOptions : ExtendableOptions
{
    /// <summary>
    /// Constructs a PublishOptions class, always setting the <see cref="Headers.StartNewTrace"/> header to <c>true</c>.
    /// </summary>
    public PublishOptions()
    {
        this.SetHeader(Headers.StartNewTrace, bool.TrueString);
    }
}