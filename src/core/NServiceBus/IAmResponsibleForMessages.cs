namespace NServiceBus
{
    /// <summary>
    /// Indicate that this endpoint holds responsibility for the given message.
    /// This could be because this endpoint publishes the message or that
    /// it is the server which processes the message.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAmResponsibleForMessages<T> { }
}
