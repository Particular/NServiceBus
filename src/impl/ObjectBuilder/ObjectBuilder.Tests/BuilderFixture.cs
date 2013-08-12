namespace ObjectBuilder.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.ObjectBuilder.Autofac;
    using NServiceBus.ObjectBuilder.CastleWindsor;
    using NServiceBus.ObjectBuilder.Ninject;
    using NServiceBus.ObjectBuilder.Spring;
    using NServiceBus.ObjectBuilder.StructureMap;
    using NServiceBus.ObjectBuilder.Unity;
    using NUnit.Framework;
    using Ninject;
    using Ninject.Extensions.ContextPreservation;
    using Ninject.Extensions.NamedScope;
    using StructureMap;
    using IContainer = NServiceBus.ObjectBuilder.Common.IContainer;

    public class BuilderFixture
    {
        protected virtual Action<IContainer> InitializeBuilder()
        {
            //no-op
            return c => { };
        }

        IList<IContainer> objectBuilders;

        protected void ForAllBuilders(Action<IContainer> assertion, params Type[] containersToIgnore)
        {
            foreach (var builder in objectBuilders.Where(b => !containersToIgnore.Contains(b.GetType())))
            {
                try
                {
                    assertion(builder);
                }
                catch (Exception)
                {
                    Console.Out.WriteLine("Assertion failed for builder: {0}", builder.GetType().Name);
                    throw;
                }
            }
        }

        [SetUp]
        public void SetUp()
        {
            objectBuilders = new List<IContainer>
                {
                    //add all supported builders here
                    new StructureMapObjectBuilder(new Container()),
                    new AutofacObjectBuilder(),
                    new WindsorObjectBuilder(),
                    new UnityObjectBuilder(),
                    new SpringObjectBuilder(),
                    new NinjectObjectBuilder(new StandardKernel(new NinjectSettings {LoadExtensions = false},
                                                                new ContextPreservationModule(), new NamedScopeModule())),
                };

            DefaultInstances.Clear();

            var initialize = InitializeBuilder();

            foreach (var builder in objectBuilders)
            {
                initialize(builder);
            }
        }

        [TearDown]
        public void DisposeContainers()
        {
            foreach (var builder in objectBuilders)
            {
                builder.Dispose();
            }
        }
    }
}