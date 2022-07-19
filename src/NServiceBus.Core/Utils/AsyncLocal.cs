#if NETFRAMEWORK
namespace System.Threading
{
    using static Runtime.Remoting.Messaging.CallContext;

    // Provides a polyfill of AsyncLocal in .NET 4.5.2
    sealed class AsyncLocal<T>
    {
        readonly string id;

        // Gets or sets the value of the ambient data.
        public T Value
        {
            get
            {
                var localValue = LogicalGetData(id);
                if (localValue != null)
                {
                    return (T)localValue;
                }
                return default;
            }
            set => LogicalSetData(id, value);
        }

        public AsyncLocal() => id = Guid.NewGuid().ToString();
    }
}
#endif