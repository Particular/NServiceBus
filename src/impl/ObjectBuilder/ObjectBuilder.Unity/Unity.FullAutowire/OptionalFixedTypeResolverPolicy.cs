using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.Practices.ObjectBuilder2;
using Microsoft.Practices.Unity;

namespace Unity.FullAutowire
{
   /// <summary>
   /// Resolver policy for resolving property dependencies which were not decorated using 
   /// <see cref="DependencyAttribute"/> attribute.
   /// </summary>
   public class OptionalFixedTypeResolverPolicy : IDependencyResolverPolicy
   {
      private readonly Type _typeToBuild;

      /// <summary>
      /// Create a new instance storing the given type.
      /// </summary>
      /// <param name="typeToBuild">Type to resolve.</param>
      public OptionalFixedTypeResolverPolicy(Type typeToBuild)
      {
         _typeToBuild = typeToBuild;
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
         IBuilderContext recursiveContext = context.CloneForNewBuild(new NamedTypeBuildKey(_typeToBuild), null);
         return recursiveContext.Strategies.ExecuteBuildUp(recursiveContext);
      }

      #endregion
   }
}
