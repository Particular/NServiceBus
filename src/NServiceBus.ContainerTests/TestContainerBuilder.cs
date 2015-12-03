namespace NServiceBus.ContainerTests
{
    using System;
    using NServiceBus.ObjectBuilder.Common;

    public static class TestContainerBuilder
    {
        public static Func<IContainer> ConstructBuilder = () => (IContainer)Activator.CreateInstance(Type.GetType("NServiceBus.AutofacObjectBuilder,NServiceBus.Core"));

    }
}