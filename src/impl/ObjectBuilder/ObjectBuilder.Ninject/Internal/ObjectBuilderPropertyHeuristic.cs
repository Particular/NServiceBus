
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
    class ObjectBuilderPropertyHeuristic : IObjectBuilderPropertyHeuristic
    {
        IList<Type> registeredTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectBuilderPropertyHeuristic"/> class.
        /// </summary>
        public ObjectBuilderPropertyHeuristic()
        {
            registeredTypes = new List<Type>();
        }

        /// <summary>
        /// Gets the registered types.
        /// </summary>
        /// <value>The registered types.</value>
        public IList<Type> RegisteredTypes
        {
            get { return registeredTypes; }
            private set { registeredTypes = value; }
        }

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

            return registeredTypes.Any(x => propertyInfo.DeclaringType.IsAssignableFrom(x))
                   && RegisteredTypes.Any(x => propertyInfo.PropertyType.IsAssignableFrom(x)) 
                   && propertyInfo.CanWrite;
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        void DisposeManaged()
        {
            if (registeredTypes != null)
            {
                registeredTypes.Clear();
            }
        }
    }
}