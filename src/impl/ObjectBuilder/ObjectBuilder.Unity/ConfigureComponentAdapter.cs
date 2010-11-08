using System;
using System.Collections.Generic;
using Microsoft.Practices.Unity;

namespace NServiceBus.ObjectBuilder.Unity
{
   public class ConfigureComponentAdapter : IComponentConfig
   {
      private readonly IUnityContainer container;
      private readonly Type concreteComponent;           
      private readonly List<InjectionMember> injectionMembers = new List<InjectionMember>();

      public ConfigureComponentAdapter(IUnityContainer container, Type concreteComponent)
      {
         this.container = container;
         this.concreteComponent = concreteComponent;         
      }

      public Type ConfiguredType
      {
         get { return concreteComponent; }
      }

      public IComponentConfig ConfigureProperty(string name, object value)
      {
         if (value != null)
         {
            var prop = new AutowireEnabledInjectionProperty(name, value);
            injectionMembers.Add(prop);
            container.Configure<InjectedMembers>().ConfigureInjectionFor(concreteComponent,
                                                                          new InjectionMember[] { prop });
         }
         return this;
      }
   }
}
