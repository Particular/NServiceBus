namespace NServiceBus.ContainerTests;

using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public class When_building_components
{
    [Test]
    public void Singleton_components_should_yield_the_same_instance()
    {
        var serviceCollection = new ServiceCollection();
        InitializeServices(serviceCollection);
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var instance1 = serviceProvider.GetService(typeof(SingletonComponent));
        var instance2 = serviceProvider.GetService(typeof(SingletonComponent));

        Assert.That(instance1, Is.EqualTo(instance2));
    }

    [Test]
    public void Transient_components_should_yield_unique_instances()
    {
        var serviceCollection = new ServiceCollection();
        InitializeServices(serviceCollection);
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var instance1 = serviceProvider.GetService<TransientComponent>();
        var instance2 = serviceProvider.GetService<TransientComponent>();

        Assert.That(instance1, Is.Not.EqualTo(instance2));
    }

    [Test]
    public void Scoped_components_should_yield_the_same_instance()
    {
        var serviceCollection = new ServiceCollection();
        InitializeServices(serviceCollection);
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var instance1 = serviceProvider.GetService(typeof(ScopedComponent));
        var instance2 = serviceProvider.GetService(typeof(ScopedComponent));

        Assert.That(instance2, Is.SameAs(instance1));
    }

    [Test]
    public void Lambda_scoped_components_should_yield_the_same_instance()
    {
        var serviceCollection = new ServiceCollection();
        InitializeServices(serviceCollection);
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var instance1 = serviceProvider.GetService(typeof(ScopedLambdaComponent));
        var instance2 = serviceProvider.GetService(typeof(ScopedLambdaComponent));

        Assert.That(instance2, Is.SameAs(instance1));
    }

    [Test]
    public void Lambda_transient_components_should_yield_unique_instances()
    {
        var serviceCollection = new ServiceCollection();
        InitializeServices(serviceCollection);
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var instance1 = serviceProvider.GetService(typeof(TransientLambdaComponent));
        var instance2 = serviceProvider.GetService(typeof(TransientLambdaComponent));

        Assert.That(instance1, Is.Not.EqualTo(instance2));
    }

    [Test]
    public void Lambda_singleton_components_should_yield_the_same_instance()
    {
        var serviceCollection = new ServiceCollection();
        InitializeServices(serviceCollection);
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var instance1 = serviceProvider.GetService(typeof(SingletonLambdaComponent));
        var instance2 = serviceProvider.GetService(typeof(SingletonLambdaComponent));

        Assert.That(instance1, Is.EqualTo(instance2));
    }

    [Test]
    public void Resolving_all_components_of_unregistered_types_should_give_empty_list()
    {
        var serviceCollection = new ServiceCollection();
        InitializeServices(serviceCollection);
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        Assert.That(serviceProvider.GetServices(typeof(UnregisteredComponent)), Is.Empty);
    }

    [Test]
    public void Resolving_recursive_types_does_not_stack_overflow()
    {
        try
        {
            var serviceCollection = new ServiceCollection();
            InitializeServices(serviceCollection);
            using var serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider.GetService(typeof(RecursiveComponent));
        }
        catch (Exception)
        {
            // this can't be a StackOverflowException as they can't be caught
        }
    }

    static void InitializeServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton(typeof(SingletonComponent));
        serviceCollection.AddTransient(typeof(TransientComponent));
        serviceCollection.AddScoped(typeof(ScopedComponent));
        serviceCollection.AddSingleton(_ => new SingletonLambdaComponent());
        serviceCollection.AddTransient(_ => new TransientLambdaComponent());
        serviceCollection.AddScoped(_ => new ScopedLambdaComponent());
        serviceCollection.AddSingleton(_ => new RecursiveComponent());
    }

    public class RecursiveComponent
    {
        public RecursiveComponent Instance { get; set; }
    }

    public class SingletonComponent
    {
    }

    public interface ISingletonComponentWithPropertyDependency
    {
    }

    public class SingletonComponentWithPropertyDependency : ISingletonComponentWithPropertyDependency
    {
        public SingletonComponent Dependency { get; set; }
    }

    public class TransientComponent
    {
    }

    public class UnregisteredComponent
    {
        public SingletonComponent SingletonComponent { get; set; }
    }

    public class SingletonLambdaComponent
    {
    }

    public class ScopedLambdaComponent
    {
    }

    public class TransientLambdaComponent
    {
    }
}

public class StaticFactory
{
#pragma warning disable CA1822 // Mark members as static
    public ComponentCreatedByFactory Create()
#pragma warning restore CA1822 // Mark members as static
    {
        return new ComponentCreatedByFactory();
    }
}

public class ComponentCreatedByFactory
{
}

public class ComponentWithBothConstructorAndSetterInjection
{
    public ComponentWithBothConstructorAndSetterInjection(ConstructorDependency constructorDependency)
    {
        ConstructorDependency = constructorDependency;
    }

    public ConstructorDependency ConstructorDependency { get; }

    public SetterDependency SetterDependency { get; set; }
}

public class ConstructorDependency
{
}

public class SetterDependency
{
}