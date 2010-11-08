using Microsoft.Practices.ObjectBuilder2;
using Microsoft.Practices.Unity;

namespace NServiceBus.ObjectBuilder.Unity
{
   /// <summary>
   /// Extension for Unity container which registeres full-autowire-enabled property resolution policy.
   /// </summary>
   public class FullAutowireContainerExtension : UnityContainerExtension
   {
      protected override void Initialize()
      {
         Context.Policies.SetDefault<IPropertySelectorPolicy>(
                new FullAutowirePropertySelectorPolicy(Container));
      }
   }
}
