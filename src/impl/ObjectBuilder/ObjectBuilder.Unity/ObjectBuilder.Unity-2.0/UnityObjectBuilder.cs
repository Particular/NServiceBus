using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.Unity;
using NServiceBus.ObjectBuilder.Common;
using Unity.FullAutowire;

namespace NServiceBus.ObjectBuilder.Unity
{
   public class UnityObjectBuilder : IContainer
   {
      /// <summary>
      /// The container itself.
      /// </summary>
      private readonly IUnityContainer _container;

      /// <summary>
      /// Instantites the class with a new UnityContainer.
      /// </summary>
      public UnityObjectBuilder()
         : this(new UnityContainer())
      {
      }

      /// <summary>
      /// Instantiates the class saving the given container.
      /// </summary>
      /// <param name="container"></param>
      public UnityObjectBuilder(IUnityContainer container)
      {
         _container = container;
         var autowireContainerExtension = _container.Configure<FullAutowireContainerExtension>();
         if (autowireContainerExtension == null)
         {
            _container.AddNewExtension<FullAutowireContainerExtension>();
         }
      }

      public object Build(Type typeToBuild)
      {
         return _container.Resolve(typeToBuild);
      }

      public IEnumerable<object> BuildAll(Type typeToBuild)
      {
         foreach (var component in _container.ResolveAll(typeToBuild))
         {
            yield return component;
         }
      }

      public void Configure(Type concreteComponent, ComponentCallModelEnum callModel)
      {
         ConfigureComponentAdapter config =
            _container.ResolveAll<ConfigureComponentAdapter>().Where(x => x.ConfiguredType == concreteComponent).
               FirstOrDefault();
         if (config == null)
         {
            IEnumerable<Type> interfaces = GetAllServiceTypesFor(concreteComponent);
            config = new ConfigureComponentAdapter(_container, concreteComponent);
            _container.RegisterInstance(Guid.NewGuid().ToString(), config);

            foreach (Type t in interfaces)
            {
               _container.RegisterType(t, concreteComponent, GetLifetimeManager(callModel));
            }
         }          
      }

      public void ConfigureProperty(Type concreteComponent, string property, object value)
      {
         ConfigureComponentAdapter config =
            _container.ResolveAll<ConfigureComponentAdapter>().Where(x => x.ConfiguredType == concreteComponent).
               First();
         config.ConfigureProperty(property, value);
      }

      public void RegisterSingleton(Type lookupType, object instance)
      {
         _container.RegisterInstance(lookupType, instance);
      }

      private static IEnumerable<Type> GetAllServiceTypesFor(Type t)
      {
         if (t == null)
         {
            return new List<Type>();
         }

         List<Type> result = new List<Type>(t.GetInterfaces());
         result.Add(t);

         foreach (Type interfaceType in t.GetInterfaces())
         {
            result.AddRange(GetAllServiceTypesFor(interfaceType));
         }

         return result;
      }

      private static LifetimeManager GetLifetimeManager(ComponentCallModelEnum callModel)
      {
         switch (callModel)
         {
            case ComponentCallModelEnum.Singlecall:
               return new TransientLifetimeManager();
            case ComponentCallModelEnum.Singleton:
               return new ContainerControlledLifetimeManager();
            default:
               return new TransientLifetimeManager();
         }

      }
   }
}
