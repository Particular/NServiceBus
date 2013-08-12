namespace NServiceBus.ObjectBuilder.Ninject.Internal
{
    using System;
    using System.Collections.Generic;
    using global::Ninject.Selection.Heuristics;

    /// <summary>
    /// Implements a heuristic for ninject property injection.
    /// </summary>
    internal interface IObjectBuilderPropertyHeuristic : IInjectionHeuristic
    {
        /// <summary>
        /// Gets the registered types.
        /// </summary>
        /// <value>The registered types.</value>
        IList<Type> RegisteredTypes
        {
            get;
        }
    }
}