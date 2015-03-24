namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

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
        public void SpecifyFirst<T>()
        {
            Types = new[] {typeof (T)};
        }

        /// <summary>
        /// Specifies an ordering of multiple types using the syntax: <code>First{H1}.Then{H2}().AndThen{H3}()</code> etc
        /// </summary>
        public void Specify<T>(First<T> ordering)
        {
            Guard.AgainstNull(ordering, "ordering");
            Types = ordering.Types;
        }

        /// <summary>
        /// Specifies an ordering of multiple types directly, where ordering may be decided dynamically at runtime.
        /// </summary>
        public void Specify(params Type[] priorityHandlers)
        {
            Guard.AgainstNullAndEmpty(priorityHandlers, "priorityHandlers");
            Types = priorityHandlers;
        }
    }
}