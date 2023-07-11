## About NServiceBus
NServiceBus is part of the [Particular Service Platform](https://particular.net/service-platform), which contains tools to build, monitor, and debug distributed systems.

With NServiceBus, you can:

- Focus on business logic, not on plumbing or infrastructure code 
- Orchestrate long-running business processes with sagas
- Run on-premises, in the cloud, in containers, or serverless
- Monitor and respond to failures using included platform tooling
- Observe system performance using Open Telemetry integration

NServiceBus includes:

- Support for messages queues using Azure Service Bus, Azure Storage Queues, Amazon SQS/SNS, RabbitMQ, and Microsoft SQL Server
- Support for storing data in Microsoft SQL Server, MySQL, PostgreSQL, Oracle, Azure Cosmos DB, Azure Table Storage, Amazon DynamoDB, MongoDB, and RavenDB
- 24x7 professional support from a team of dedicated engineers located around the world

## Getting started

- Visit the [NServiceBus Quick Start](https://docs.particular.net/tutorials/quickstart/) to learn how NServiceBus helps you build better software systems.
- Visit the [NServiceBus step-by-step tutorial](https://docs.particular.net/tutorials/nservicebus-step-by-step/) to learn how to build NServiceBus systems, including how to send commands, publish events, manage multiple message endpoints, and retry failed messages.
- Install the [ParticularTemplates NuGet package](https://www.nuget.org/packages/ParticularTemplates) to get NServiceBus templates to bootstrap projects using either `dotnet new` or in Visual Studio.
- Check out our other [tutorials](https://docs.particular.net/tutorials/) and [samples](https://docs.particular.net/samples/).
- Get [help with a proof-of-concept](https://particular.net/proof-of-concept).

## Packages

Find links to [all our NuGet packages](https://docs.particular.net/nservicebus/platform-nuget-packages) in our documentation.

## Support

- Browse our [documentation](https://docs.particular.net).
- Reach out to the [ParticularDiscussion](https://discuss.particular.net/) community.
- [Contact us](https://particular.net/support) to discuss your support requirements.

## Building

To build NServiceBus, open `NServiceBus.sln` in Visual Studio and build the solution.

You'll find the built assemblies in /binaries.

If you see the build failing, check that you haven't put the source of NServiceBus in a deep subdirectory since long path names (greater than 248 characters) aren't supported by MSBuild.

## Licensing

### NServiceBus

NServiceBus is licensed under the RPL 1.5 license. More details can be found [here](LICENSE.md).

### [net-object-deep-copy](https://github.com/Burtsev-Alexey/net-object-deep-copy)

net-object-deep-copy is licensed under the MIT license as described [here](https://github.com/Burtsev-Alexey/net-object-deep-copy/blob/master/README).

net-object-deep-copy sources are compiled into the NServiceBus distribution as allowed under the license terms found [here](https://github.com/Burtsev-Alexey/net-object-deep-copy/blob/master/README).

### [FastExpressionCompiler](https://github.com/dadhi/FastExpressionCompiler)

FastExpressionCompiler is licensed under the MIT license as described [here](https://github.com/dadhi/FastExpressionCompiler/blob/master/LICENSE).

FastExpressionCompiler sources are compiled into the NServiceBus distribution as allowed under the license terms found [here](https://github.com/dadhi/FastExpressionCompiler/blob/master/LICENSE).
