#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using Sagas;

record SagaMapping(IReadOnlyList<SagaFinderDefinition> Finders, ISagaNotFoundHandlerInvocation NotFoundHandler, SagaMetadata.CorrelationPropertyMetadata? CorrelationProperty);