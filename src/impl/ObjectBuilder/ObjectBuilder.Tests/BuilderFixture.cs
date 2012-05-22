using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NServiceBus.ObjectBuilder.Autofac;
using NServiceBus.ObjectBuilder.CastleWindsor;
using NServiceBus.ObjectBuilder.Ninject;
using NServiceBus.ObjectBuilder.Spring;
using NServiceBus.ObjectBuilder.StructureMap;
using NServiceBus.ObjectBuilder.Unity;
using NUnit.Framework;
using StructureMap;
using IContainer=NServiceBus.ObjectBuilder.Common.IContainer;
using Ninject;
using Ninject.Extensions.ContextPreservation;
using Ninject.Extensions.NamedScope;

namespace ObjectBuilder.Tests
{
    public class BuilderFixture
    {
        protected virtual Action<IContainer> InitializeBuilder()
        {
            //no-op
            return (c) => { };
        }

        private IList<IContainer> objectBuilders;

        protected void ForAllBuilders(Action<IContainer> assertion,params Type[] containersToIgnore)
        {
            bool failed = false;

            foreach (var builder in objectBuilders.Where(b=>!containersToIgnore.Contains(b.GetType())))
            {
                try
                {
                    assertion(builder);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Test failed for: " + builder.GetType().Name);
                    Trace.WriteLine(ex);
                    failed = true;
                }
            }
            Assert.False(failed,"One or more of the builders failed");
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
                                     new NinjectObjectBuilder(new StandardKernel(new NinjectSettings{LoadExtensions = false}, new ContextPreservationModule(), new NamedScopeModule())),
                                 };

            DefaultInstances.Clear();

            var inilialize = InitializeBuilder();

            foreach (var builder in objectBuilders)
            {
                inilialize(builder);
            }
        }
    }
}