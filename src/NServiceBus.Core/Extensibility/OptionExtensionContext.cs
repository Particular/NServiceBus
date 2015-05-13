namespace NServiceBus.Extensibility
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Holds data added by various extentions
    /// </summary>
    public class OptionExtensionContext
    {

        Dictionary<string, object> context = new Dictionary<string, object>();

        /// <summary>
        /// Stores the given data in the extension context
        /// </summary>
        /// <param name="data">The actual data</param>
        /// <typeparam name="T">The T of data to store</typeparam>
        public void Set<T>(T data) where T : class
        {
            if (!typeof(T).IsNested)
            {
                throw new InvalidOperationException("The context needs to be a nested class. Most likely you would add a class named `Context` to the behavior where you need to access the context data");    
            }

            context[typeof(T).FullName] = data;
        }

        /// <summary>
        /// Gets the specified extension data
        /// </summary>
        /// <typeparam name="T">The extension type to get</typeparam>
        /// <param name="data">The data if found</param>
        /// <returns>True if found</returns>
        public bool TryGet<T>(out T data) where T:class
        {
            object value;

            if(context.TryGetValue(typeof(T).FullName, out value))
            {
                data = (T) value;
                return true;
            }

            data = null;
            return false;
        }

        /// <summary>
        /// Gets the requested extesion, a new one will be created if needed
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetOrCreate<T>() where T:class,new()
        {
            T value;

            if (TryGet(out value))
            {
                return value;
            }
            var newInstance = Activator.CreateInstance<T>();
            Set(newInstance);

            return newInstance;
        }
    }
}