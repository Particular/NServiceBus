using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ninject;

namespace NServiceBus.ObjectBuilder.Ninject.Internal
{
    /// <summary>
    /// Implements an more aggressive injection heuristic.
    /// </summary>
    internal class ObjectBuilderPropertyHeuristic : IObjectBuilderPropertyHeuristic
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectBuilderPropertyHeuristic"/> class.
        /// </summary>
        public ObjectBuilderPropertyHeuristic()
        {
            this.RegisteredTypes = new List<Type>();
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
        public INinjectSettings Settings
        {
            get;
            set;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Determines whether a given type should be injected.
        /// </summary>
        /// <param name="member">The member info to check.</param>
        /// <returns><see langword="true"/> if a given type needs to be injected; otherwise <see langword="false"/>.
        /// </returns>
        public bool ShouldInject(MemberInfo member)
        {
            var propertyInfo = member as PropertyInfo;

            if (propertyInfo == null)
            {
                return false;
            }

            var shouldInject = this.RegisteredTypes.Where(x => propertyInfo.DeclaringType.IsAssignableFrom(x)).Any()
                   && this.RegisteredTypes.Where(x => propertyInfo.PropertyType.IsAssignableFrom(x)).Any() 
                   && propertyInfo.CanWrite;

            return shouldInject;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release
        /// only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            this.RegisteredTypes.Clear();
        }
    }
}