namespace NServiceBus.Core.Tests.Features;

using System;
using System.Runtime.CompilerServices;
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
        featureFactory = new FakeFeatureFactory();
        featureSettings = new FeatureComponent.Settings(featureFactory);
        settings.Set(featureSettings);
        featureComponent = new FeatureComponent(featureSettings);
    }

    [Test]
    public async Task Should_start_and_stop_features()
    {
        var feature = new FeatureWithStartupTask();

        featureFactory.Add(feature);

        featureSettings.EnableFeature<FeatureWithStartupTask>();

        featureComponent.SetupFeatures(new FakeFeatureConfigurationContext(), settings);

        await featureComponent.StartFeatures(null, null);
        await featureComponent.StopFeatures(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(feature.TaskStarted, Is.True);
            Assert.That(feature.TaskStopped, Is.True);
        }
    }

    [Test]
    public async Task Should_start_and_stop_features_in_dependency_order()
    {
        var orderBuilder = new StringBuilder();

        var featureWithStartupTaskWithDependency = new FeatureWithStartupTaskWithDependency(orderBuilder);
        var featureWithStartupThatAnotherFeatureDependsOn = new FeatureWithStartupThatAnotherFeatureDependsOn(orderBuilder);

        featureFactory.Add(featureWithStartupTaskWithDependency, featureWithStartupThatAnotherFeatureDependsOn);

        featureSettings.EnableFeature<FeatureWithStartupTaskWithDependency>();
        featureSettings.EnableFeature<FeatureWithStartupThatAnotherFeatureDependsOn>();

        featureComponent.SetupFeatures(new FakeFeatureConfigurationContext(), settings);

        await featureComponent.StartFeatures(null, null);
        await featureComponent.StopFeatures(null);

        var expectedOrderBuilder = new StringBuilder();
        expectedOrderBuilder.AppendLine("FeatureWithStartupThatAnotherFeatureDependsOn.Start");
        expectedOrderBuilder.AppendLine("FeatureWithStartupTaskWithDependency.Start");
        expectedOrderBuilder.AppendLine("FeatureWithStartupThatAnotherFeatureDependsOn.Stop");
        expectedOrderBuilder.AppendLine("FeatureWithStartupTaskWithDependency.Stop");

        Assert.That(orderBuilder.ToString(), Is.EqualTo(expectedOrderBuilder.ToString()));
    }

    [Test]
    public async Task Should_dispose_feature_startup_tasks_when_they_implement_IDisposable()
    {
        var feature = new FeatureWithStartupTaskWhichIsDisposable();

        featureFactory.Add(feature);

        featureSettings.EnableFeature<FeatureWithStartupTaskWhichIsDisposable>();

        featureComponent.SetupFeatures(new FakeFeatureConfigurationContext(), settings);

        await featureComponent.StartFeatures(null, null);
        await featureComponent.StopFeatures(null);

        Assert.That(feature.TaskDisposed, Is.True);
    }

    [Test]
    public async Task Should_dispose_feature_startup_tasks_when_they_implement_IAsyncDisposable()
    {
        var feature = new FeatureWithStartupTaskWhichIsAsyncDisposable();

        featureFactory.Add(feature);

        featureSettings.EnableFeature<FeatureWithStartupTaskWhichIsAsyncDisposable>();

        featureComponent.SetupFeatures(new FakeFeatureConfigurationContext(), settings);

        await featureComponent.StartFeatures(null, null);
        await featureComponent.StopFeatures(null);

        Assert.That(feature.TaskDisposed, Is.True);
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

        featureComponent.SetupFeatures(new FakeFeatureConfigurationContext(), settings);

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await featureComponent.StartFeatures(null, null));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.Message, Is.EqualTo("feature2"));

            Assert.That(feature1.TaskStarted, Is.True, "Feature 1 should have been started.");
            Assert.That(feature2.TaskStartCalled, Is.True, "An attempt should have been made to start feature 2.");
            Assert.That(feature1.TaskStopCalled, Is.True, "An attempt should have been made to stop feature 1.");
            Assert.That(feature2.TaskStopCalled, Is.False, "No attempt should have been made to stop feature 2.");
            Assert.That(feature3.TaskStartCalled, Is.False, "No attempt should have been made to start feature 3.");
        }
    }

    [Test]
    public async Task Should_not_throw_when_feature_task_fails_on_stop_and_should_not_abort_stopping()
    {
        var feature1 = new FeatureWithStartupTaskThatThrows(throwOnStart: false, throwOnStop: false);
        var feature2 = new FeatureWithStartupTaskThatThrows(throwOnStart: false, throwOnStop: true);

        featureSettings.Add(feature1);
        featureSettings.Add(feature2);

        featureComponent.SetupFeatures(new FakeFeatureConfigurationContext(), settings);

        await featureComponent.StartFeatures(null, null);

        Assert.DoesNotThrowAsync(async () => await featureComponent.StopFeatures(null));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(feature1.TaskStarted && feature1.TaskStopped, Is.True);
            Assert.That(feature2.TaskStarted && !feature2.TaskStopped, Is.True);
        }
    }

    [Test]
    public async Task Should_dispose_feature_task_even_when_stop_throws()
    {
        var feature = new FeatureWithStartupTaskThatThrows(throwOnStart: false, throwOnStop: true);
        featureSettings.Add(feature);

        featureComponent.SetupFeatures(new FakeFeatureConfigurationContext(), settings);

        await featureComponent.StartFeatures(null, null);

        await featureComponent.StopFeatures(null);
        Assert.That(feature.TaskDisposed, Is.True);
    }

    FeatureComponent.Settings featureSettings;
    SettingsHolder settings;
    FakeFeatureFactory featureFactory;
    FeatureComponent featureComponent;

    class FeatureWithStartupTaskWithDependency : TestFeature
    {
        public FeatureWithStartupTaskWithDependency() : this(new StringBuilder())
        {
        }

        public FeatureWithStartupTaskWithDependency(StringBuilder orderBuilder)
        {
            DependsOn<FeatureWithStartupThatAnotherFeatureDependsOn>();

            this.orderBuilder = orderBuilder;
        }

        protected override void Setup(FeatureConfigurationContext context) => context.RegisterStartupTask(new Runner(orderBuilder));

        class Runner(StringBuilder orderBuilder) : FeatureStartupTask
        {
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
        }

        readonly StringBuilder orderBuilder;
    }

    class FeatureWithStartupThatAnotherFeatureDependsOn(StringBuilder orderBuilder) : TestFeature
    {
        public FeatureWithStartupThatAnotherFeatureDependsOn() : this(new StringBuilder())
        {
        }

        protected override void Setup(FeatureConfigurationContext context) => context.RegisterStartupTask(new Runner(orderBuilder));

        class Runner(StringBuilder orderBuilder) : FeatureStartupTask
        {
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
        }
    }

    class FeatureWithStartupTask : TestFeature
    {
        public bool TaskStarted { get; private set; }
        public bool TaskStopped { get; private set; }

        protected override void Setup(FeatureConfigurationContext context) => context.RegisterStartupTask(new Runner(this));

        public class Runner(FeatureWithStartupTask parentFeature) : FeatureStartupTask
        {
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
        }
    }

    class FeatureWithStartupTaskThatThrows : TestFeature
    {
        public FeatureWithStartupTaskThatThrows(bool throwOnStart = false, bool throwOnStop = false, Func<Exception> createException = null, [CallerArgumentExpression(nameof(throwOnStart))] string throwOnStartExpression = null, [CallerArgumentExpression(nameof(throwOnStop))] string throwOnStopExpression = null)
        {
            this.throwOnStart = throwOnStart;
            this.throwOnStop = throwOnStop;
            this.createException = createException ?? (() => new InvalidOperationException());

            IsEnabled = true;

            // This is a hack to bypass the restriction that the same feature type can't be added twice
            Name = $"{GetType().FullName}.{throwOnStartExpression}.{throwOnStopExpression}";
        }

        public bool TaskStartCalled { get; private set; }
        public bool TaskStarted { get; private set; }
        public bool TaskStopCalled { get; private set; }
        public bool TaskStopped { get; private set; }
        public bool TaskDisposed { get; private set; }

        protected override void Setup(FeatureConfigurationContext context) => context.RegisterStartupTask(new Runner(this));

        readonly bool throwOnStart;
        readonly bool throwOnStop;
        readonly Func<Exception> createException;

        public class Runner(FeatureWithStartupTaskThatThrows parentFeature) : FeatureStartupTask, IDisposable
        {
            public void Dispose()
            {
                parentFeature.TaskDisposed = true;
                GC.SuppressFinalize(this);
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
        }
    }

    class FeatureWithStartupTaskWhichIsDisposable : TestFeature
    {
        public bool TaskDisposed { get; private set; }

        protected override void Setup(FeatureConfigurationContext context) => context.RegisterStartupTask(new Runner(this));

        sealed class Runner(FeatureWithStartupTaskWhichIsDisposable parentFeature) : FeatureStartupTask, IDisposable
        {
            public void Dispose() => parentFeature.TaskDisposed = true;

            protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }

    class FeatureWithStartupTaskWhichIsAsyncDisposable : TestFeature
    {
        public bool TaskDisposed { get; private set; }

        protected override void Setup(FeatureConfigurationContext context) => context.RegisterStartupTask(new Runner(this));

        sealed class Runner(FeatureWithStartupTaskWhichIsAsyncDisposable parentFeature) : FeatureStartupTask, IAsyncDisposable
        {
            public ValueTask DisposeAsync()
            {
                parentFeature.TaskDisposed = true;
                return ValueTask.CompletedTask;
            }

            protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}