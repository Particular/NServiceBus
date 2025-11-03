namespace NServiceBus.Tests;

using System;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public class TypeRegistrationsTests
{
    [Test]
    public void RegisterExtensionType_Should_Store_Types_Correctly()
    {
        // Arrange
        var typeRegistrations = new TypeRegistrations();

        // Act - Register different types of extensions
        typeRegistrations.RegisterExtensionType<IEvent, TestEvent>();
        typeRegistrations.RegisterExtensionType<ICommand, TestCommand>();
        typeRegistrations.RegisterExtensionType<IHandleMessages, TestHandler>();
        typeRegistrations.RegisterExtensionType<IHandleMessages, TestHandler2>(); // Multiple handlers

        // Assert - Verify types are stored correctly
        var eventTypes = typeRegistrations.GetAvailableTypes(typeof(IEvent)).ToList();
        var commandTypes = typeRegistrations.GetAvailableTypes(typeof(ICommand)).ToList();
        var handlerTypes = typeRegistrations.GetAvailableTypes(typeof(IHandleMessages)).ToList();
        var allTypes = typeRegistrations.GetAllRegisteredTypes().ToList();

        Assert.Multiple(() =>
        {
            // Events
            Assert.That(eventTypes, Has.Count.EqualTo(1));
            Assert.That(eventTypes, Contains.Item(typeof(TestEvent)));

            // Commands
            Assert.That(commandTypes, Has.Count.EqualTo(1));
            Assert.That(commandTypes, Contains.Item(typeof(TestCommand)));

            // Handlers (multiple)
            Assert.That(handlerTypes, Has.Count.EqualTo(2));
            Assert.That(handlerTypes, Contains.Item(typeof(TestHandler)));
            Assert.That(handlerTypes, Contains.Item(typeof(TestHandler2)));

            // All types
            Assert.That(allTypes, Has.Count.EqualTo(4));
            Assert.That(allTypes, Contains.Item(typeof(TestEvent)));
            Assert.That(allTypes, Contains.Item(typeof(TestCommand)));
            Assert.That(allTypes, Contains.Item(typeof(TestHandler)));
            Assert.That(allTypes, Contains.Item(typeof(TestHandler2)));
        });
    }

    [Test]
    public void GetAvailableTypes_Should_Return_Empty_For_Unregistered_Marker()
    {
        // Arrange
        var typeRegistrations = new TypeRegistrations();

        // Act
        var result = typeRegistrations.GetAvailableTypes(typeof(IEvent));

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetAllRegisteredTypes_Should_Return_Empty_When_No_Types_Registered()
    {
        // Arrange
        var typeRegistrations = new TypeRegistrations();

        // Act
        var result = typeRegistrations.GetAllRegisteredTypes();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void RegisterHandler_Should_Store_Handler_To_Message_Mapping()
    {
        // Arrange
        var typeRegistrations = new TypeRegistrations();

        // Act - Register handlers with their specific message types
        typeRegistrations.RegisterHandler<TestHandler, TestEvent>();
        typeRegistrations.RegisterHandler<TestHandler2, TestCommand>();

        // Assert - Verify handler → message mapping
        var eventHandlerMessages = typeRegistrations.GetMessageTypesForHandler(typeof(TestHandler)).ToList();
        var commandHandlerMessages = typeRegistrations.GetMessageTypesForHandler(typeof(TestHandler2)).ToList();
        var allHandlers = typeRegistrations.GetAvailableTypes(typeof(IHandleMessages)).ToList();

        Assert.Multiple(() =>
        {
            // Handler → Message mapping
            Assert.That(eventHandlerMessages, Has.Count.EqualTo(1));
            Assert.That(eventHandlerMessages, Contains.Item(typeof(TestEvent)));

            Assert.That(commandHandlerMessages, Has.Count.EqualTo(1));
            Assert.That(commandHandlerMessages, Contains.Item(typeof(TestCommand)));

            // Extension point registration
            Assert.That(allHandlers, Has.Count.EqualTo(2));
            Assert.That(allHandlers, Contains.Item(typeof(TestHandler)));
            Assert.That(allHandlers, Contains.Item(typeof(TestHandler2)));
        });
    }

    // Test types
    public class TestEvent : IEvent { }
    public class TestCommand : ICommand { }
    public class TestHandler : IHandleMessages<TestEvent>
    {
        public System.Threading.Tasks.Task Handle(TestEvent message, IMessageHandlerContext context) =>
            System.Threading.Tasks.Task.CompletedTask;
    }
    public class TestHandler2 : IHandleMessages<TestCommand>
    {
        public System.Threading.Tasks.Task Handle(TestCommand message, IMessageHandlerContext context) =>
            System.Threading.Tasks.Task.CompletedTask;
    }
}
