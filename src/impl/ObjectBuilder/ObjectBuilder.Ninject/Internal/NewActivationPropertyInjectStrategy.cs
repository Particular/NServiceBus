using System;
using System.Collections.Generic;
using Ninject.Activation;
using Ninject.Activation.Strategies;
using Ninject.Infrastructure;
using Ninject.Injection;

namespace NServiceBus.ObjectBuilder.Ninject.Internal
{


    /// <summary>
    /// Only injects properties on an instance if that instance has not 
    /// been previously activated.  This forces property injection to occur 
    /// only once for instances within a scope -- e.g. singleton or within 
    /// the same request, etc.  Instances are removed on deactivation.
    /// </summary>
    internal class NewActivationPropertyInjectStrategy : PropertyInjectionStrategy
    {
        private readonly HashSet<object> activatedInstances = new HashSet<object>();

        /// <summary>
        /// Initializes a new instance of the <see cref="NewActivationPropertyInjectStrategy"/> class.
        /// </summary>
        /// <param name="injectorFactory">The injector factory component.</param>
        public NewActivationPropertyInjectStrategy(IInjectorFactory injectorFactory)
            : base(injectorFactory)
        {
        }

        /// <summary>
        /// Injects values into the properties as described by 
        /// <see cref="T:Ninject.Planning.Directives.PropertyInjectionDirective"/>s
        /// contained in the plan.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="reference">A reference to the instance being 
        /// activated.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="context"/> parameter is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="reference"/> parameter is <c>null</c>.</exception>
        public override void Activate(IContext context, InstanceReference reference)
        {
            if (this.activatedInstances.Contains(reference.Instance))
            {
                return; // "Skip" standard activation as it was already done!
            }

            // Keep track of non-transient activations...  
            // Note: Maybe this should be 
            //       ScopeCallback == StandardScopeCallbacks.Singleton
            if (context.Binding.ScopeCallback != StandardScopeCallbacks.Transient)
            {
                this.activatedInstances.Add(reference.Instance);
            }

            base.Activate(context, reference);
        }

        /// <summary>
        /// Contributes to the deactivation of the instance in the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="reference">A reference to the instance being 
        /// deactivated.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="reference"/> parameter is <c>null</c>.</exception>
        public override void Deactivate(IContext context, InstanceReference reference)
        {
            this.activatedInstances.Remove(reference.Instance);
            base.Deactivate(context, reference);
        }
    }
}