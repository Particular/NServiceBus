namespace NServiceBus.ContainerTests
{
    using System;
    using ObjectBuilder.Common;

    public static class TestContainerBuilder
    {
        public static Func<IContainer> ConstructBuilder = () => (IContainer)Activator.CreateInstance(Type.GetType("NServiceBus.LightInjectObjectBuilder, NServiceBus.Core"));

    }
}