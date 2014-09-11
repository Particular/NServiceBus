namespace ObjectBuilder.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Ninject;
    using Ninject.Extensions.ContextPreservation;
    using Ninject.Extensions.NamedScope;
    using NServiceBus.ObjectBuilder.Autofac;
    using NServiceBus.ObjectBuilder.CastleWindsor;
    using NServiceBus.ObjectBuilder.Ninject;
    using NServiceBus.ObjectBuilder.Spring;
    using NUnit.Framework;
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
                    new AutofacObjectBuilder(),
                    new WindsorObjectBuilder(),
                    new SpringObjectBuilder(),
                    new NinjectObjectBuilder(new StandardKernel(new NinjectSettings {LoadExtensions = false},
                                                                new ContextPreservationModule(), new NamedScopeModule())),
                };

            var initialize = InitializeBuilder();

            foreach (var builder in objectBuilders)
            {
                initialize(builder);
            }
        }

        [TearDown]
        public void DisposeContainers()
        {
            if (objectBuilders != null)
            {
                foreach (var builder in objectBuilders)
                {
                    builder.Dispose();
                }
            }
        }
    }
}