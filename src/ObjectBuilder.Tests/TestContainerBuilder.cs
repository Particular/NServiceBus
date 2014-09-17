namespace ObjectBuilder.Tests
{
    using System;
    using NServiceBus.ObjectBuilder.Common;

    public static class TestContainerBuilder
    {
        public static Func<IContainer> ConstructBuilder = () => new NServiceBus.ObjectBuilder.Autofac.AutofacObjectBuilder();

    }
}