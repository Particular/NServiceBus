using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NServiceBus.ObjectBuilder.Autofac;
using NServiceBus.ObjectBuilder.CastleWindsor;
using NServiceBus.ObjectBuilder.StructureMap;
using NServiceBus.ObjectBuilder.Unity;
using NUnit.Framework;
using StructureMap;
using IContainer=NServiceBus.ObjectBuilder.Common.IContainer;

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

        protected void VerifyForAllBuilders(Action<IContainer> assertion,params Type[] containersToIgnore)
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
                    Debug.WriteLine("Test failed for: " + builder.GetType().Name);
                    Debug.WriteLine(ex);
                    failed = true;
                }
            }
            Assert.False(failed,"One or more of the builers failed");
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
                                     new UnityObjectBuilder()
                                 };

            var inilialize = InitializeBuilder();

            foreach (var builder in objectBuilders)
            {
                inilialize(builder);
            }
        }
    }
}