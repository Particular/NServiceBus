using System;
using System.Collections.Generic;

namespace NServiceBus
{
    /// <summary>
    /// Specify the order in which message handlers will be invoked.
    /// </summary>
    public interface ISpecifyMessageHandlerOrdering
    {
        /// <summary>
        /// In this method, use the order object to specify the order 
        /// in which message handlers will be activated.
        /// </summary>
        /// <param name="order"></param>
        void SpecifyOrder(Order order);
    }

    /// <summary>
    /// Used to specify the order in which message handlers will be activated.
    /// </summary>
    public class Order
    {
        ///<summary>
        /// Gets the types whose order has been specified.
        ///</summary>
        public IEnumerable<Type> Types { get; set; }


        /// <summary>
        /// Specifies that the given type will be activated before all others.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void SpecifyFirst<T>()
        {
            Types = new[] {typeof (T)};
        }

        /// <summary>
        /// Obselete - use SpecifyFirst instead.
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        [ObsoleteEx(Replacement = "SpecifyFirst<T>", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public void Specify<TFirst>()
        {
            SpecifyFirst<TFirst>();
        }

        /// <summary>
        /// Specifies an ordering of multiple types using the syntax:
        /// First{H1}.Then{H2}().AndThen{H3}()... etc
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ordering"></param>
        public void Specify<T>(First<T> ordering)
        {
            Types = ordering.Types;
        }
    }

    /// <summary>
    /// Used to indicate the order in which handler types are to run.
    /// 
    /// Not thread safe.
    /// </summary>
    /// <typeparam name="T">The type which will run first.</typeparam>
    public class First<T>
    {
        /// <summary>
        /// Specifies the type which will run next.
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <returns></returns>
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
        /// Specifies the type which will run next
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <returns></returns>
        public First<T> AndThen<K>()
        {
            if (!types.Contains(typeof(K)))
                types.Add(typeof(K));

            return this;
        }

        private readonly IList<Type> types = new List<Type>();
    }
}
