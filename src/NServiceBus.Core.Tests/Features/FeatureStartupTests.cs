namespace NServiceBus.Core.Tests.Features
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class FeatureStartupTests
    {
        [SetUp]
        public void Init()
        {
            settings = new SettingsHolder();
            featureSettings = new FeatureActivator(settings);
        }

        [Test]
        public async Task Should_start_and_stop_features()
        {
            var feature = new FeatureWithStartupTask();

            featureSettings.Add(feature);

            featureSettings.SetupFeatures(new FakeFeatureConfigurationContext());

            await featureSettings.StartFeatures(null, null);
            await featureSettings.StopFeatures();

            Assert.True(feature.TaskStarted);
            Assert.True(feature.TaskStopped);
        }

        [Test]
        public async Task Should_start_and_stop_features_in_dependency_order()
        {
            var orderBuilder = new StringBuilder();

            featureSettings.Add(new FeatureWithStartupTaskWithDependency(orderBuilder));
            featureSettings.Add(new FeatureWithStartupThatAnotherFeatureDependsOn(orderBuilder));

            featureSettings.SetupFeatures(new FakeFeatureConfigurationContext());

            await featureSettings.StartFeatures(null, null);
            await featureSettings.StopFeatures();

            var expectedOrderBuilder = new StringBuilder();
            expectedOrderBuilder.AppendLine("FeatureWithStartupThatAnotherFeatureDependsOn.Start");
            expectedOrderBuilder.AppendLine("FeatureWithStartupTaskWithDependency.Start");
            expectedOrderBuilder.AppendLine("FeatureWithStartupThatAnotherFeatureDependsOn.Stop");
            expectedOrderBuilder.AppendLine("FeatureWithStartupTaskWithDependency.Stop");

            Assert.AreEqual(expectedOrderBuilder.ToString(), orderBuilder.ToString());
        }

        [Test]
        public async Task Should_dispose_feature_startup_tasks_when_they_implement_IDisposable()
        {
            var feature = new FeatureWithStartupTaskWhichIsDisposable();

            featureSettings.Add(feature);

            featureSettings.SetupFeatures(new FakeFeatureConfigurationContext());

            await featureSettings.StartFeatures(null, null);
            await featureSettings.StopFeatures();

            Assert.True(feature.TaskDisposed);
        }

        [Test]
        public void Should_throw_when_feature_task_fails_on_start_and_should_stop_previously_started_tasks_and_should_abort_starting()
        {
            var feature1 = new FeatureWithStartupTaskThatThrows(throwOnStop: true, createException: () => new InvalidOperationException("feature1"));
            var feature2 = new FeatureWithStartupTaskThatThrows(throwOnStart: true, createException: () => new InvalidOperationException("feature2"));
            var feature3 = new FeatureWithStartupTaskThatThrows();

            featureSettings.Add(feature1);
            featureSettings.Add(feature2);
            featureSettings.Add(feature3);

            featureSettings.SetupFeatures(new FakeFeatureConfigurationContext());

            var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await featureSettings.StartFeatures(null, null));
            Assert.AreEqual("feature2", exception.Message);

            Assert.True(feature1.TaskStarted, "Feature 1 should have been started.");
            Assert.True(feature2.TaskStartCalled, "An attempt should have been made to start feature 2.");
            Assert.True(feature1.TaskStopCalled, "An attempt should have been made to stop feature 1.");
            Assert.False(feature2.TaskStopCalled, "No attempt should have been made to stop feature 2.");
            Assert.False(feature3.TaskStartCalled, "No attempt should have been made to start feature 3.");
        }

        [Test]
        public async Task Should_not_throw_when_feature_task_fails_on_stop_and_should_not_abort_stopping()
        {
            var feature1 = new FeatureWithStartupTaskThatThrows(throwOnStart: false, throwOnStop: false);
            var feature2 = new FeatureWithStartupTaskThatThrows(throwOnStart: false, throwOnStop: true);

            featureSettings.Add(feature1);
            featureSettings.Add(feature2);

            featureSettings.SetupFeatures(new FakeFeatureConfigurationContext());

            await featureSettings.StartFeatures(null, null);

            Assert.DoesNotThrowAsync(async () => await featureSettings.StopFeatures());

            Assert.True(feature1.TaskStarted && feature1.TaskStopped);
            Assert.True(feature2.TaskStarted && !feature2.TaskStopped);
        }

        [Test]
        public async Task Should_dispose_feature_task_even_when_stop_throws()
        {
            var feature = new FeatureWithStartupTaskThatThrows(throwOnStart: false, throwOnStop: true);
            featureSettings.Add(feature);

            featureSettings.SetupFeatures(new FakeFeatureConfigurationContext());

            await featureSettings.StartFeatures(null, null);

            await featureSettings.StopFeatures();
            Assert.True(feature.TaskDisposed);
        }

        FeatureActivator featureSettings;
        SettingsHolder settings;

        class FeatureWithStartupTaskWithDependency : TestFeature
        {
            public FeatureWithStartupTaskWithDependency(StringBuilder orderBuilder)
            {
                EnableByDefault();
                DependsOn<FeatureWithStartupThatAnotherFeatureDependsOn>();

                this.orderBuilder = orderBuilder;
            }

            protected internal override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(new Runner(orderBuilder));
            }

            class Runner : FeatureStartupTask
            {
                public Runner(StringBuilder orderBuilder)
                {
                    this.orderBuilder = orderBuilder;
                }

                protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                {
                    orderBuilder.AppendLine($"{nameof(FeatureWithStartupTaskWithDependency)}.Start");
                    return Task.CompletedTask;
                }

                protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
                {
                    orderBuilder.AppendLine($"{nameof(FeatureWithStartupTaskWithDependency)}.Stop");
                    return Task.CompletedTask;
                }

                StringBuilder orderBuilder;
            }

            readonly StringBuilder orderBuilder;
        }

        class FeatureWithStartupThatAnotherFeatureDependsOn : TestFeature
        {
            public FeatureWithStartupThatAnotherFeatureDependsOn(StringBuilder orderBuilder)
            {
                EnableByDefault();

                this.orderBuilder = orderBuilder;
            }

            protected internal override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(new Runner(orderBuilder));
            }

            class Runner : FeatureStartupTask
            {
                public Runner(StringBuilder orderBuilder)
                {
                    this.orderBuilder = orderBuilder;
                }

                protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                {
                    orderBuilder.AppendLine($"{nameof(FeatureWithStartupThatAnotherFeatureDependsOn)}.Start");
                    return Task.CompletedTask;
                }

                protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
                {
                    orderBuilder.AppendLine($"{nameof(FeatureWithStartupThatAnotherFeatureDependsOn)}.Stop");
                    return Task.CompletedTask;
                }

                StringBuilder orderBuilder;
            }

            readonly StringBuilder orderBuilder;
        }

        class FeatureWithStartupTask : TestFeature
        {
            public FeatureWithStartupTask()
            {
                EnableByDefault();
            }

            public bool TaskStarted { get; private set; }
            public bool TaskStopped { get; private set; }

            protected internal override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(new Runner(this));
            }

            public class Runner : FeatureStartupTask
            {
                public Runner(FeatureWithStartupTask parentFeature)
                {
                    this.parentFeature = parentFeature;
                }

                protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                {
                    parentFeature.TaskStarted = true;
                    return Task.CompletedTask;
                }

                protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
                {
                    parentFeature.TaskStopped = true;
                    return Task.CompletedTask;
                }

                FeatureWithStartupTask parentFeature;
            }
        }

        class FeatureWithStartupTaskThatThrows : TestFeature
        {
            public FeatureWithStartupTaskThatThrows(bool throwOnStart = false, bool throwOnStop = false, Func<Exception> createException = null)
            {
                this.throwOnStart = throwOnStart;
                this.throwOnStop = throwOnStop;
                this.createException = createException ?? (() => new InvalidOperationException());

                EnableByDefault();
            }

            public bool TaskStartCalled { get; private set; }
            public bool TaskStarted { get; private set; }
            public bool TaskStopCalled { get; private set; }
            public bool TaskStopped { get; private set; }
            public bool TaskDisposed { get; private set; }

            protected internal override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(new Runner(this));
            }

            bool throwOnStart;
            bool throwOnStop;
            Func<Exception> createException;

            public class Runner : FeatureStartupTask, IDisposable
            {
                public Runner(FeatureWithStartupTaskThatThrows parentFeature)
                {
                    this.parentFeature = parentFeature;
                }

                public void Dispose()
                {
                    parentFeature.TaskDisposed = true;
                }

                protected override async Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                {
                    parentFeature.TaskStartCalled = true;

                    await Task.Yield();

                    if (parentFeature.throwOnStart)
                    {
                        throw parentFeature.createException();
                    }

                    parentFeature.TaskStarted = true;
                }

                protected override async Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
                {
                    parentFeature.TaskStopCalled = true;

                    await Task.Yield();

                    if (parentFeature.throwOnStop)
                    {
                        throw parentFeature.createException();
                    }

                    parentFeature.TaskStopped = true;
                }

                FeatureWithStartupTaskThatThrows parentFeature;
            }
        }

        class FeatureWithStartupTaskWhichIsDisposable : TestFeature
        {
            public FeatureWithStartupTaskWhichIsDisposable()
            {
                EnableByDefault();
            }

            public bool TaskDisposed { get; private set; }

            protected internal override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(new Runner(this));
            }

            public class Runner : FeatureStartupTask, IDisposable
            {
                public Runner(FeatureWithStartupTaskWhichIsDisposable parentFeature)
                {
                    this.parentFeature = parentFeature;
                }

                public void Dispose()
                {
                    parentFeature.TaskDisposed = true;
                }

                protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                {
                    return Task.CompletedTask;
                }

                protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
                {
                    return Task.CompletedTask;
                }

                FeatureWithStartupTaskWhichIsDisposable parentFeature;
            }
        }
    }

    public class FakeBuilder : IServiceProvider
    {
        public FakeBuilder(Type type)
        {
            this.type = type;
        }

        public object GetService(Type serviceType)
        {
            if (serviceType != type)
            {
                throw new Exception("Not the expected type");
            }
            return Activator.CreateInstance(serviceType);
        }

        Type type;
    }
}