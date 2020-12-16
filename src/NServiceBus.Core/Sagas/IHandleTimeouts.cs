namespace NServiceBus
{
    using System.Threading.Tasks;

    /// <summary>
    /// Tells the infrastructure that the user wants to handle a timeout of <typeparamref name="T" />.
    /// </summary>
    public interface IHandleTimeouts<T>
    {
    }
}