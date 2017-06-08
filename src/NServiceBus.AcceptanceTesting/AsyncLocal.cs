#if NET452
namespace System.Threading
{
    using Runtime.Remoting.Messaging;

    /// <summary>
    /// Provides a polyfill of AsyncLocal in .NET 4.5.2
    /// </summary>
    /// <typeparam name="T"></typeparam>
    sealed class AsyncLocal<T>
    {
        readonly string id;

        /// <summary>Gets or sets the value of the ambient data. </summary>
        /// <returns>The value of the ambient data. </returns>
        public T Value
        {
            get
            {
                var localValue = CallContext.LogicalGetData(id);
                if (localValue != null)
                    return (T)localValue;
                return default(T);
            }
            set
            {
                // ReSharper disable once ArrangeAccessorOwnerBody
                CallContext.LogicalSetData(id, value);
            }
        }

        public AsyncLocal()
        {
            id = Guid.NewGuid().ToString();
        }
    }
}
#endif