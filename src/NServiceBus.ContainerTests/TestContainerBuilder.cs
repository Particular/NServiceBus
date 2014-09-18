namespace NServiceBus.ContainerTests
{
    using System;
    using NServiceBus.ObjectBuilder.Common;

    public static class TestContainerBuilder
    {
        public static Func<IContainer> ConstructBuilder = () => { return (IContainer)Activator.CreateInstance(Type.GetType("NServiceBus.ObjectBuilder.Autofac.AutofacObjectBuilder,NServiceBus.Core")); };

    }
}