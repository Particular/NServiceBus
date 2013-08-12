using Microsoft.Practices.Unity;
using NServiceBus.ObjectBuilder.Unity;

namespace NServiceBus
{
    using ObjectBuilder.Common.Config;

    /// <summary>
   /// Contains extension methods for configuring object builder infrastructure through Unity container.
   /// </summary>
   public static class ConfigureUnityBuilder
   {
      /// <summary>
      /// Use the Unity builder.
      /// </summary>
      public static Configure UnityBuilder(this Configure config)
      {
         ConfigureCommon.With(config, new UnityObjectBuilder());
         return config;
      }

      /// <summary>
      /// Use the Unity builder passing in a pre-configured container to be used by nServiceBus.
      /// </summary>
      public static Configure UnityBuilder(this Configure config, IUnityContainer container)
      {
         ConfigureCommon.With(config, new UnityObjectBuilder(container));
         return config;
      }      
   }
}
