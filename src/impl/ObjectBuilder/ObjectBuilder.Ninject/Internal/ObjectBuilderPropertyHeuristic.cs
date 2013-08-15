
namespace NServiceBus.ObjectBuilder.Ninject.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using global::Ninject;

    /// <summary>
    /// Implements an more aggressive injection heuristic.
    /// </summary>
    internal class ObjectBuilderPropertyHeuristic : IObjectBuilderPropertyHeuristic
    {
        bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectBuilderPropertyHeuristic"/> class.
        /// </summary>
        public ObjectBuilderPropertyHeuristic()
        {
            RegisteredTypes = new List<Type>();
        }

        /// <summary>
        /// Gets the registered types.
        /// </summary>
        /// <value>The registered types.</value>
        public IList<Type> RegisteredTypes { get; private set; }

        /// <summary>
        /// Gets or sets the settings.
        /// </summary>
        /// <value>The settings.</value>
        public INinjectSettings Settings{get;set;}

        /// <summary>
        /// Determines whether a given type should be injected.
        /// </summary>
        /// <param name="member">The member info to check.</param>
        /// <returns><see langword="true"/> if a given type needs to be injected; otherwise <see langword="false"/>.</returns>
        public bool ShouldInject(MemberInfo member)
        {
            var propertyInfo = member as PropertyInfo;

            if (propertyInfo == null || propertyInfo.GetSetMethod(Settings.InjectNonPublic) == null)
            {
                return false;
            }

            var shouldInject = RegisteredTypes.Any(x => propertyInfo.DeclaringType.IsAssignableFrom(x))
                   && RegisteredTypes.Any(x => propertyInfo.PropertyType.IsAssignableFrom(x)) 
                   && propertyInfo.CanWrite;

            return shouldInject;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (RegisteredTypes != null)
            {
                RegisteredTypes.Clear();
            }

            disposed = true;
        }
    }
}