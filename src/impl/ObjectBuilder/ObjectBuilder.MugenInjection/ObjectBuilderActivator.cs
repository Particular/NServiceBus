using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MugenInjection.Activators;
using MugenInjection;

namespace NServiceBus.ObjectBuilder.MugenInjection
{
    /// <summary>
    /// Represents the activator for <see cref="MugenInjectionObjectBuilder"/>.
    /// </summary>
    internal class ObjectBuilderActivator : ExpressionActivator
    {
        #region Fields

        private Type _serviceType;
        private List<PropertyInfo> _cachedProperty;

        #endregion

        #region Overrides of ActivatorBase

        /// <summary>
        /// Gets a properties for inject.
        /// </summary>
        /// <param name="service">The specified service type.</param>
        /// <param name="attributeType">The specified attribute for inject type.</param>
        /// <returns>A properties for inject.</returns>
        protected override IList<PropertyInfo> GetPropertyForInject(Type service, Type attributeType)
        {
            IList<PropertyInfo> propertyForInject = base.GetPropertyForInject(service, attributeType);
            foreach (PropertyInfo propertyInfo in GetProperty(service))
            {
                if (propertyForInject.Contains(propertyInfo)) continue;
                if (CurrentContext.Parameters.Any(parameter => parameter.CanResolve(propertyInfo, null)))
                {
                    propertyForInject.Add(propertyInfo);
                    continue;
                }
                if (!CurrentContext.Injector.CanResolve(propertyInfo.PropertyType, true, false)) continue;
                propertyForInject.Add(propertyInfo);
            }
            return propertyForInject;
        }

        #endregion

        #region Method

        private IEnumerable<PropertyInfo> GetProperty(Type service)
        {
            if (_serviceType != service)
            {
                _serviceType = service;
                _cachedProperty = service.GetProperties(GetBindingFlags()).ToList();
                return _cachedProperty;
            }
            return _cachedProperty;
        }

        #endregion
    }
}