using System;
using System.Collections.Generic;
using Ninject.Selection.Heuristics;

namespace NServiceBus.ObjectBuilder.Ninject.Internal
{
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