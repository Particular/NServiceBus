namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Used to indicate the order in which handler types are to run.
    /// 
    /// Not thread safe.
    /// </summary>
    /// <typeparam name="T">The type which will run first.</typeparam>
    [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", ReplacementTypeOrMember = "BusConfiguration.ExecuteTheseHandlersFirst")]
    public class First<T>
    {
        /// <summary>
        /// Specifies the type which will run next.
        /// </summary>
        public static First<T> Then<K>()
        {
            var instance = new First<T>();

            instance.AndThen<T>();
            instance.AndThen<K>();

            return instance;
        }

        /// <summary>
        /// Returns the ordered list of types specified.
        /// </summary>
        public IEnumerable<Type> Types
        {
            get { return types; }
        }

        /// <summary>
        /// Specifies the type which will run next.
        /// </summary>
        public First<T> AndThen<K>()
        {
            if (!types.Contains(typeof(K)))
                types.Add(typeof(K));

            return this;
        }

        List<Type> types = new List<Type>();
    }
}