namespace NServiceBus.ContainerTests;

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

[TestFixture]
public class When_querying_for_registered_components
{
    [Test]
    public void Existing_components_should_return_true()
    {
        var serviceCollection = new ServiceCollection();
        InitializeBuilder(serviceCollection);

        Assert.That(serviceCollection.Any(sd => sd.ServiceType == typeof(ExistingComponent)), Is.True);
    }

    [Test]
    public void Non_existing_components_should_return_false()
    {
        var serviceCollection = new ServiceCollection();
        InitializeBuilder(serviceCollection);

        Assert.That(serviceCollection.Any(sd => sd.ServiceType == typeof(NonExistingComponent)), Is.False);
    }

    [Test]
    public void Builders_should_not_determine_existence_by_building_components()
    {
        var serviceCollection = new ServiceCollection();
        InitializeBuilder(serviceCollection);

        Assert.That(serviceCollection.Any(sd => sd.ServiceType == typeof(ExistingComponentWithUnsatisfiedDependency)), Is.True);
    }

    static void InitializeBuilder(IServiceCollection c)
    {
        c.AddTransient<ExistingComponent>();
        c.AddTransient<ExistingComponentWithUnsatisfiedDependency>();
    }

    public class NonExistingComponent
    {
    }

    public class ExistingComponent
    {
    }

    public class ExistingComponentWithUnsatisfiedDependency
    {
        public ExistingComponentWithUnsatisfiedDependency(NonExistingComponent dependency)
        {

        }
    }
}