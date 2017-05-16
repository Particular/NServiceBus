namespace NServiceBus.Sagas.Orchestrations
{
    /// <summary>
    /// Markup interface for request, improving type inference in c#.
    /// </summary>
    /// <typeparam name="TReply"></typeparam>
    public interface IRequest<TReply>
    {
    }
}