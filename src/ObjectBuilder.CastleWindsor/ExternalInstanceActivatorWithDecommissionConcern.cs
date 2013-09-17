namespace NServiceBus.ObjectBuilder.CastleWindsor
{
    using Castle.Core;
    using Castle.MicroKernel;
    using Castle.MicroKernel.ComponentActivator;
    using Castle.MicroKernel.Context;

    class ExternalInstanceActivatorWithDecommissionConcern : AbstractComponentActivator, IDependencyAwareActivator
    {
        public ExternalInstanceActivatorWithDecommissionConcern(ComponentModel model, IKernelInternal kernel, ComponentInstanceDelegate onCreation, ComponentInstanceDelegate onDestruction)
            : base(model, kernel, onCreation, onDestruction)
        {
        }

        public bool CanProvideRequiredDependencies(ComponentModel component)
        {
            //we already have an instance so we don't need to provide any dependencies at all
            return true;
        }

        public bool IsManagedExternally(ComponentModel component)
        {
            return false;
        }

        protected override object InternalCreate(CreationContext context)
        {
            return Model.ExtendedProperties["instance"];
        }

        protected override void InternalDestroy(object instance)
        {
            ApplyDecommissionConcerns(instance);
        }
    }
}