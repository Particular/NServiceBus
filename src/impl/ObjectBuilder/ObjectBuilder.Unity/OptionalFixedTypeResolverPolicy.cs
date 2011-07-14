using System;
using Microsoft.Practices.ObjectBuilder2;
using Microsoft.Practices.Unity;

namespace NServiceBus.ObjectBuilder.Unity
{
   /// <summary>
   /// Resolver policy for resolving property dependencies which were not decorated using 
   /// <see cref="DependencyAttribute"/> attribute.
   /// </summary>
   public class OptionalFixedTypeResolverPolicy : IDependencyResolverPolicy
   {
      private readonly Type typeToBuild;

      /// <summary>
      /// Create a new instance storing the given type.
      /// </summary>
      /// <param name="typeToBuild">Type to resolve.</param>
      public OptionalFixedTypeResolverPolicy(Type typeToBuild)
      {
         this.typeToBuild = typeToBuild;
      }

      #region IDependencyResolverPolicy Members

      /// <summary>
      /// Get the value for a dependency.
      /// </summary>
      /// <param name="context">Current build context.</param>
      /// <returns>The value for the dependency.</returns>      
      public object Resolve(IBuilderContext context)
      {
         if (context == null)
         {
            throw new ArgumentNullException("context");
         }         
         IBuilderContext recursiveContext = context.CloneForNewBuild(new NamedTypeBuildKey(typeToBuild), null);
         return recursiveContext.Strategies.ExecuteBuildUp(recursiveContext);
      }

      #endregion
   }
}
