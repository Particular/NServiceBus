namespace NServiceBus.Core.Tests.Features
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NServiceBus.Transports;
    using NUnit.Framework;
    using ObjectBuilder;
    using Settings;

    [TestFixture]
    public class FeatureStartupTests
    {
        private FeatureActivator featureSettings;
        private SettingsHolder settings;

        [SetUp]
        public void Init()
        {
            settings = new SettingsHolder();
            settings.Set<TransportDefinition>(new MsmqTransport());
            featureSettings = new FeatureActivator(settings);
        }

        [Test]
        public async Task Should_start_and_stop_features()
        {
            var feature = new FeatureWithStartupTask();
       
            featureSettings.Add(feature);

            var builder = new FakeBuilder(typeof(FeatureWithStartupTask.Runner));

            featureSettings.SetupFeatures(null, null);

            await featureSettings.StartFeatures(builder, null);
            await featureSettings.StopFeatures(null);

            Assert.True(FeatureWithStartupTask.Runner.Started);
            Assert.True(FeatureWithStartupTask.Runner.Stopped);
        }

        [Test]
        public async Task Should_dispose_feature_when_they_implement_IDisposable()
        {
            var feature = new FeatureWithStartupTaskWhichIsDisposable();

            featureSettings.Add(feature);

            var builder = new FakeBuilder(typeof(FeatureWithStartupTaskWhichIsDisposable.Runner));

            featureSettings.SetupFeatures(null, null);

            await featureSettings.StartFeatures(builder, null);
            await featureSettings.StopFeatures(null);

            Assert.True(FeatureWithStartupTaskWhichIsDisposable.Runner.Disposed);
        }

        class FeatureWithStartupTask : TestFeature
        {
            public FeatureWithStartupTask()
            {
                EnableByDefault();
            }

            protected internal override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(new Runner());
            }

            public class Runner : FeatureStartupTask
            {
                protected override Task OnStart(IMessageSession session)
                {
                    Started = true;
                    return TaskEx.CompletedTask;
                }

                protected override Task OnStop(IMessageSession session)
                {
                    Stopped = true;
                    return TaskEx.CompletedTask;
                }

                public static bool Started { get; private set; }
                public static bool Stopped { get; private set; }
            }
        }

        class FeatureWithStartupTaskWhichIsDisposable : TestFeature
        {
            public FeatureWithStartupTaskWhichIsDisposable()
            {
                EnableByDefault();
            }

            protected internal override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(new Runner());
            }

            public class Runner : FeatureStartupTask, IDisposable
            {
                protected override Task OnStart(IMessageSession session)
                {
                    return TaskEx.CompletedTask;
                }

                protected override Task OnStop(IMessageSession session)
                {
                    return TaskEx.CompletedTask;
                }

                public void Dispose()
                {
                    Disposed = true;
                }

                public static bool Disposed { get; private set; }
            }
        }
    }

    public class FakeBuilder : IBuilder
    {
        Type type;

        public FakeBuilder()
        {
            
        }
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
                throw new Exception("Not the expected type");
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