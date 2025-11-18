#if NET9_0_OR_GREATER

namespace NServiceBus.AcceptanceTests.Core.JsonSerializer;

using System.Text.Json;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_using_system_json_serializer_with_options : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_use_the_provided_json_options()
    {
        var context = Scenario.Define<Context>()
           .WithEndpoint<Endpoint>(c => c
               .When(b => b.SendLocal(new MyMessage())))
           .Done(c => c.GotTheMessage);

        var ex = Assert.ThrowsAsync<JsonException>(async () => await context.Run());
        Assert.That(ex.Message, Does.Match("^The property or field.*doesn't allow getting null values.*$"));
    }

    public class Context : ScenarioContext
    {
        public bool GotTheMessage { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint()
        {

            EndpointSetup<DefaultServer>((c, r) =>
            {
                var serialization = c.UseSerialization<SystemJsonSerializer>();
                JsonSerializerOptions options = new()
                {
                    RespectNullableAnnotations = true
                };
                serialization.Options(options);
            });
        }

        class MyHandler : IHandleMessages<MyMessage>
        {
            Context testContext;

            public MyHandler(Context testContext) => this.testContext = testContext;

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.GotTheMessage = true;
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage
    {
#nullable enable
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public string RequiredNullable { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    }
}
#endif