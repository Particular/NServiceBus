#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Sagas;

// Core-owned types can't rely on the user's JSON source generation context.
[JsonSerializable(typeof(SagaTimeoutTrigger))]
[JsonSerializable(typeof(List<StoredSagaTimeout>))]
sealed partial class SagaTimeoutJsonContext : JsonSerializerContext;
