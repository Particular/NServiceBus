using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Practices.ObjectBuilder2;
using System.Reflection;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.ObjectBuilder;

namespace Unity.FullAutowire
{
   /// <summary>
   /// Property selection policy which replaces Unity default property selection policy with 
   /// algorithm that returns all the properties which
   /// were configured by hand (using InjectedMembers), by Dependency attribute and werend configured 
   /// (optional property dependencies).
   /// </summary>
   /// <remarks>
   /// Optional property dependencies can only be of reference types.
   /// </remarks>
   public class FullAutowirePropertySelectorPolicy : IPropertySelectorPolicy
   {
      private readonly IUnityContainer _container;
      private readonly IEnumerable<Type> _attributesToIgnore;
      private readonly DefaultUnityPropertySelectorPolicy _defaultProlicy = new DefaultUnityPropertySelectorPolicy();
      private readonly SpecifiedPropertiesSelectorPolicy _specifiedPropertiesPolicy = new SpecifiedPropertiesSelectorPolicy();

      /// <summary>
      /// Creates new policy object for using in provided container.
      /// </summary>      
      public FullAutowirePropertySelectorPolicy(IUnityContainer container)
      {
         _container = container;
         _attributesToIgnore = new [] { typeof(DependencyAttribute)};
      }

      /// <summary>
      /// Object that handles configured by-hand properties.
      /// </summary>
      internal SpecifiedPropertiesSelectorPolicy SpecifiedPropertiesPolicy
      {
         get { return _specifiedPropertiesPolicy; }
      }

      /// <summary>
      /// Clones this policy object.
      /// </summary>
      /// <returns></returns>
      internal FullAutowirePropertySelectorPolicy Clone()
      {
         return new FullAutowirePropertySelectorPolicy(_container);
      }
      
      public IEnumerable<SelectedProperty> SelectProperties(IBuilderContext context)
      {
         Type t = BuildKey.GetType(context.BuildKey);
         HashSet<string> propertyNames = new HashSet<string>();
         foreach (SelectedProperty prop in _specifiedPropertiesPolicy.SelectProperties(context))
         {
            if (!propertyNames.Contains(prop.Property.Name))
            {
               propertyNames.Add(prop.Property.Name);
               yield return prop;
            }
         }
         foreach (SelectedProperty prop in _defaultProlicy.SelectProperties(context))
         {
            if (!propertyNames.Contains(prop.Property.Name))
            {
               yield return prop;
            }
         }

         foreach (PropertyInfo prop in t.GetProperties(BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.Instance))
         {
            if (prop.GetIndexParameters().Length == 0 &&               
               prop.CanWrite && !ShoudBeIgnored(prop) &&
               !propertyNames.Contains(prop.Name) &&
               !prop.PropertyType.IsValueType && 
               CanBeResolved(prop))                                      
            {
               yield return CreateSelectedProperty(context, prop);               
            }
         }         
      }

      private bool CanBeResolved(PropertyInfo info)
      {
         try
         {
            _container.Resolve(info.PropertyType);
            return true;
         }
         catch (ResolutionFailedException)
         {
            return false;
         }
      }

      private bool ShoudBeIgnored(PropertyInfo info)
      {
         foreach (Type attributeType in _attributesToIgnore)
         {
            if (info.IsDefined(attributeType, false))
            {
               return true;
            }
         }
         return false;
      }

      private static SelectedProperty CreateSelectedProperty(IBuilderContext context, PropertyInfo property)
      {
         string key = Guid.NewGuid().ToString();
         SelectedProperty result = new SelectedProperty(property, key);
         context.PersistentPolicies.Set<IDependencyResolverPolicy>(new OptionalFixedTypeResolverPolicy(property.PropertyType), key);
         DependencyResolverTrackerPolicy.TrackKey(context.PersistentPolicies,
             context.BuildKey,
             key);
         return result;
      }      
   }
}
