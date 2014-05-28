namespace NServiceBus.Core.Tests.Features
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Features;
    using NUnit.Framework;
    using ObjectBuilder;
    using Settings;

    [TestFixture]
    public class FeatureStartupTests
    {
        [Test]
        public void Should_not_activate_features_with_unmet_dependencies()
        {
            var feature = new FeatureWithStartupTask();
       
            var featureSettings = new FeatureActivator(new SettingsHolder());

            featureSettings.Add(feature);
       
            featureSettings.SetupFeatures();

            featureSettings.StartFeatures(new FakeBuilder(typeof(FeatureWithStartupTask.Runner)));
            Assert.True(FeatureWithStartupTask.Runner.Started);
        }



        class FeatureWithStartupTask : TestFeature
        {
            public FeatureWithStartupTask()
            {
                EnableByDefault();
                RegisterStartupTask<Runner>();
            }

            public class Runner:FeatureStartupTask
            {
                protected override void OnStart()
                {
                    Started = true;
                }

                public static bool Started { get; set; }
            }
        }


    }

    public class FakeBuilder : IBuilder
    {
        readonly Type type;

        public FakeBuilder(Type type)
        {
            this.type = type;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public object Build(Type typeToBuild)
        {
            if (typeToBuild != type)
            {
                throw new Exception("Not the expected task");
            }
            return Activator.CreateInstance(typeToBuild);
        }

        public IBuilder CreateChildBuilder()
        {
            throw new NotImplementedException();
        }

        public T Build<T>()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> BuildAll<T>()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            throw new NotImplementedException();
        }

        public void Release(object instance)
        {
            throw new NotImplementedException();
        }

        public void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            throw new NotImplementedException();
        }
    }
}