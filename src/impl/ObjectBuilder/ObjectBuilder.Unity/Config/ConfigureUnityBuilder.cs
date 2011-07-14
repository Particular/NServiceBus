using Microsoft.Practices.Unity;
using NServiceBus.ObjectBuilder.Common.Config;
using NServiceBus.ObjectBuilder.Unity;

namespace NServiceBus
{
   /// <summary>
   /// Contains extension methods for configuring object builder infrastructure through Unity container.
   /// </summary>
   public static class ConfigureUnityBuilder
   {
      /// <summary>
      /// Use the Unity builder.
      /// </summary>
      /// <param name="config"></param>
      /// <returns></returns>
      public static Configure UnityBuilder(this Configure config)
      {
         ConfigureCommon.With(config, new UnityObjectBuilder());
         return config;
      }

      /// <summary>
      /// Use the Unity builder passing in a preconfigured container to be used by nServiceBus.
      /// </summary>
      /// <param name="config"></param>
      /// <param name="container"></param>
      /// <returns></returns>
      public static Configure UnityBuilder(this Configure config, IUnityContainer container)
      {
         ConfigureCommon.With(config, new UnityObjectBuilder(container));
         return config;
      }      
   }
}
