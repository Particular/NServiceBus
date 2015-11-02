﻿namespace NServiceBus.Core.Tests.Features
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NUnit.Framework;
    using ObjectBuilder;
    using Settings;

    [TestFixture]
    public class FeatureStartupTests
    {
        [Test]
        public async Task Should_start_and_stop_features()
        {
            var feature = new FeatureWithStartupTask();
       
            var featureSettings = new FeatureActivator(new SettingsHolder());

            featureSettings.Add(feature);

            var builder = new FakeBuilder(typeof(FeatureWithStartupTask.Runner));

            featureSettings.SetupFeatures(new FeatureConfigurationContext(null, null, null));

            await featureSettings.StartFeatures(builder, null);
            await featureSettings.StopFeatures(builder, null);

            Assert.True(FeatureWithStartupTask.Runner.Started);
            Assert.True(FeatureWithStartupTask.Runner.Stopped);
        }

        [Test]
        public async Task Should_dispose_feature_when_they_implement_IDisposable()
        {
            var feature = new FeatureWithStartupTaskWhichIsDisposable();

            var featureSettings = new FeatureActivator(new SettingsHolder());

            featureSettings.Add(feature);

            var builder = new FakeBuilder(typeof(FeatureWithStartupTaskWhichIsDisposable.Runner));

            featureSettings.SetupFeatures(new FeatureConfigurationContext(null, null, null));

            await featureSettings.StartFeatures(builder, null);
            await featureSettings.StopFeatures(builder, null);

            Assert.True(FeatureWithStartupTaskWhichIsDisposable.Runner.Disposed);
        }

        class FeatureWithStartupTask : TestFeature
        {
            public FeatureWithStartupTask()
            {
                EnableByDefault();
                RegisterStartupTask<Runner>();
            }

            public class Runner : FeatureStartupTask
            {
                protected override Task OnStart(IBusContext context)
                {
                    Started = true;
                    return Task.FromResult(0);
                }

                protected override Task OnStop(IBusContext context)
                {
                    Stopped = true;
                    return Task.FromResult(0);
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
                RegisterStartupTask<Runner>();
            }

            public class Runner : FeatureStartupTask, IDisposable
            {
                protected override Task OnStart(IBusContext context)
                {
                    return Task.FromResult(0);
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