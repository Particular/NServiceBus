namespace NServiceBus.ObjectBuilder.CastleWindsor
{
    using Castle.Core.Resource;
    using Castle.MicroKernel;
    using Castle.MicroKernel.SubSystems.Configuration;
    using Castle.Windsor.Configuration.Interpreters;

    class NoOpInterpreter : AbstractInterpreter
    {
        public override void ProcessResource(IResource resource, IConfigurationStore store, IKernel kernel)
        {

        }
    }
}