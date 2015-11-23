namespace NServiceBus.Pipeline
{
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an inlet to a pipe.
    /// </summary>
    /// <typeparam name="T">Context type.</typeparam>
    public interface IPipeInlet<in T>
    {
        /// <summary>
        /// Puts a given context into that pipe.
        /// </summary>
        Task Put(T context);
    }
}