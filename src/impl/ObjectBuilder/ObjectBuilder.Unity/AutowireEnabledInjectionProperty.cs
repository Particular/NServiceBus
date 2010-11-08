using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Practices.ObjectBuilder2;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.ObjectBuilder;

namespace NServiceBus.ObjectBuilder.Unity
{
   /// <summary>
   /// Specialy prepared derivative of <see cref="InjectionProperty"/> adjusted to cooperate with 
   /// autowire infrastructure. Does not replace property selector policy for 
   /// <see cref="SpecifiedPropertiesSelectorPolicy"/> but, instead, uses 
   /// <see cref="FullAutowirePropertySelectorPolicy"/>'s
   /// <see cref="FullAutowirePropertySelectorPolicy.SpecifiedPropertiesPolicy"/>.
   /// TODO: This class needs cleaning up the exception throwing code to include more information
   /// for users.
   /// </summary>
   /// <remarks>
   /// Based on Unity's <see cref="InjectionProperty"/> class.
   /// </remarks>   
   public class AutowireEnabledInjectionProperty : InjectionMember
   {
      private readonly string propertyName;
      private InjectionParameterValue parameterValue;

      /// <summary>
      /// Configure the container to inject the given property name,
      /// resolving the value via the container.
      /// </summary>
      /// <param name="propertyName">Name of the property to inject.</param>
      public AutowireEnabledInjectionProperty(string propertyName)
      {
         this.propertyName = propertyName;
      }

      /// <summary>
      /// Configure the container to inject the given property name,
      /// using the value supplied. This value is converted to an
      /// <see cref="InjectionParameterValue"/> object using the
      /// rules defined by the <see cref="InjectionParameterValue.ToParameters"/>
      /// method.
      /// </summary>
      /// <param name="propertyName">Name of property to inject.</param>
      /// <param name="propertyValue">Value for property.</param>
      public AutowireEnabledInjectionProperty(string propertyName, object propertyValue)
      {
         this.propertyName = propertyName;
         parameterValue = InjectionParameterValue.ToParameter(propertyValue);
      }
      /// <summary>
      /// Add policies to the <paramref name="policies"/> to configure the
      /// container to call this constructor with the appropriate parameter values.
      /// </summary>
      /// <param name="typeToCreate">Type to register.</param>
      /// <param name="name">Name used to resolve the type object.</param>
      /// <param name="policies">Policy list to add policies to.</param>
      [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods",
          Justification = "Validation is done via Guard class")]
      public override void AddPolicies(Type typeToCreate, string name, IPolicyList policies)
      {
         Guard.ArgumentNotNull(typeToCreate, "typeToCreate");
         PropertyInfo propInfo = typeToCreate.GetProperty(propertyName);
         GuardPropertyExists(propInfo, typeToCreate, propertyName);
         GuardPropertyIsSettable(propInfo);
         GuardPropertyIsNotIndexer(propInfo);
         InitializeParameterValue(propInfo);
         GuardPropertyValueIsCompatible(propInfo, parameterValue);

         SpecifiedPropertiesSelectorPolicy selector =
             GetSelectorPolicy(policies, typeToCreate, name);

         selector.AddPropertyAndValue(propInfo, parameterValue);
      }

      private InjectionParameterValue InitializeParameterValue(PropertyInfo propInfo)
      {
         if (parameterValue == null)
         {
            parameterValue = new ResolvedParameter(propInfo.PropertyType);
         }
         return parameterValue;
      }
      
      private static SpecifiedPropertiesSelectorPolicy GetSelectorPolicy(IPolicyList policies, Type typeToInject, string name)
      {
         NamedTypeBuildKey key = new NamedTypeBuildKey(typeToInject, name);
         IPropertySelectorPolicy selector =
             policies.GetNoDefault<IPropertySelectorPolicy>(key, false);

         if (selector == null)
         {
            FullAutowirePropertySelectorPolicy defaultSelector =
               policies.Get<IPropertySelectorPolicy>(key, false) as FullAutowirePropertySelectorPolicy;
            if (defaultSelector != null)
            {
               selector = defaultSelector.Clone();
               policies.Set(selector, key);
            }
            else
            {
               throw new InvalidOperationException("Cannot use AutiviewEnabledInjectionProperty without FullAutowireContainerExtension. Please register FullAutowireContainerExtension extension in the container.");
            }
         }
         return ((FullAutowirePropertySelectorPolicy)selector).SpecifiedPropertiesPolicy;
      }

      private static void GuardPropertyExists(PropertyInfo propInfo, Type typeToCreate, string propertyName)
      {
         if (propInfo == null)
         {
            throw new InvalidOperationException();
         }
      }

      private static void GuardPropertyIsSettable(PropertyInfo propInfo)
      {
         if (!propInfo.CanWrite)
         {
            throw new InvalidOperationException();
         }
      }

      private static void GuardPropertyIsNotIndexer(PropertyInfo property)
      {
         if (property.GetIndexParameters().Length > 0)
         {
            throw new InvalidOperationException();
         }
      }
      private static void GuardPropertyValueIsCompatible(PropertyInfo property, InjectionParameterValue value)
      {
         if (!value.MatchesType(property.PropertyType))
         {
            throw new InvalidOperationException();
         }
      }     
   }
}
