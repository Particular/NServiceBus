#nullable enable

namespace NServiceBus;

using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Sagas;
using Serialization;
using Unicast.Messages;

/// <summary>
/// The saga data that will be persisted.
/// </summary>
public interface IContainSagaData
{
    /// <summary>
    /// Gets/sets the Id of the process. Do NOT generate this value in saga code.
    /// The value of the Id will be generated automatically to provide the
    /// best performance for saving in a database.
    /// </summary>
    /// <remarks>
    /// The reason Guid is used for process Id is that messages containing this Id need
    /// to be sent by the process even before it is persisted.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    Guid Id { get; set; }

    /// <summary>
    /// Contains the return address of the endpoint that caused the process to run.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    string? Originator { get; set; }

    /// <summary>
    /// Contains the Id of the message which caused the saga to start.
    /// This is needed so that when we reply to the Originator, any
    /// registered callbacks will be fired correctly.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    string? OriginalMessageId { get; set; }
}

/// <summary>
/// Informs that the saga data contains information about the timeouts
/// </summary>
public interface IContainSagaTimeoutData : IContainSagaData
{
    /// <summary>
    /// Serialized timeout data collection
    /// </summary>
    List<TimeoutInfo> TimeoutData { get; set; }
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="TTimeoutMessageType"></typeparam>
public class TimeoutReference<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] TTimeoutMessageType>
{
    readonly TimeoutInfo info;
    readonly Saga saga;
    readonly TimeoutData timeoutData;

    /// <summary>
    /// Unique ID of this timeout
    /// </summary>
    public string TimeoutId => info.TimeoutId;

    /// <summary>
    /// Due time estimated based on the delay used to trigger the timeout
    /// </summary>
    public DateTimeOffset? EstimatedDueTime => info.EstimatedDueTime;

    /// <summary>
    /// Due time requested
    /// </summary>
    public DateTimeOffset? ExactDueTime => info.ExactDueTime;

    /// <summary>
    /// State of the timeout
    /// </summary>
    public TTimeoutMessageType State { get; private set; }

    internal TimeoutReference(TimeoutInfo info, Saga saga, TimeoutData timeoutData)
    {
        this.info = info;
        this.saga = saga;
        this.timeoutData = timeoutData;

        State = timeoutData.Deserialize<TTimeoutMessageType>(info);
    }

    /// <summary>
    /// Cancels this timeout.
    /// </summary>
    public void Cancel()
    {
        //TODO: This may fail if cancel is called while enumerating the TimeoutReferences returned by FindTimeouts
        timeoutData.Remove(TimeoutId);
    }

    /// <summary>
    /// Changes the due date of this timeout.
    /// </summary>
    /// <param name="newDueDate"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public Task Reschedule(DateTimeOffset newDueDate, IMessageHandlerContext context)
    {
        var options = new SendOptions();

        options.DoNotDeliverBefore(newDueDate);
        options.RouteToThisEndpoint();

        saga.SetTimeoutHeaders(options);

        var triggerId = Guid.NewGuid().ToString();
        options.SetHeader("NServiceBus.SagaTimeoutTriggerID", triggerId);
        options.SetHeader("NServiceBus.SagaTimeoutID", TimeoutId);

        info.TriggerId = triggerId;
        info.ExactDueTime = newDueDate;
        info.EstimatedDueTime = null;

        return context.Send<TTimeoutMessageType>(State, options);
    }

    /// <summary>
    /// Changes the state of a timeout
    /// </summary>
    /// <param name="newState"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public Task Update(TTimeoutMessageType newState, IMessageHandlerContext context)
    {
        var options = new SendOptions();

        //HINT: If TimeSpan was originally used to defer, we need to convert to absolute time based on the estimated trigger time
        var at = info.ExactDueTime ?? info.EstimatedDueTime!.Value;
        options.DoNotDeliverBefore(at);
        options.RouteToThisEndpoint();

        saga.SetTimeoutHeaders(options);

        var triggerId = Guid.NewGuid().ToString();
        options.SetHeader("NServiceBus.SagaTimeoutTriggerID", triggerId);
        options.SetHeader("NServiceBus.SagaTimeoutID", TimeoutId);

        info.TriggerId = triggerId;
        info.ExactDueTime = at;
        info.EstimatedDueTime = null;

        State = newState;
        timeoutData.Serialize(newState, info);

        return context.Send<TTimeoutMessageType>(State, options);
    }
}

class TimeoutData
{
    readonly List<TimeoutInfo> timeoutInfos;
    readonly MessageDeserializerResolver deserializerResolver;
    readonly IMessageSerializer mainSerializer;
    readonly MessageMetadataRegistry messageMetadataRegistry;

    public TimeoutData(List<TimeoutInfo> timeoutInfos, MessageDeserializerResolver deserializerResolver, IMessageSerializer mainSerializer, MessageMetadataRegistry messageMetadataRegistry)
    {
        this.timeoutInfos = timeoutInfos;
        this.deserializerResolver = deserializerResolver;
        this.mainSerializer = mainSerializer;
        this.messageMetadataRegistry = messageMetadataRegistry;
    }

    public void Serialize<T>(T state, TimeoutInfo info)
    {
        info.Type = typeof(T).AssemblyQualifiedName!;
        info.ContentType = mainSerializer.ContentType;

        using var stream = new MemoryStream();

        mainSerializer.Serialize(state!, stream);

        var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length);
        info.Payload = base64;
    }

    public T Deserialize<T>(TimeoutInfo info)
    {
        //TODO: Use resolver
        byte[] bytes = Convert.FromBase64String(info.Payload);
        using var stream = new MemoryStream(bytes);
        var metadata = messageMetadataRegistry.GetMessageMetadata(info.Type);
        var payload = mainSerializer.Deserialize(bytes, metadata.MessageHierarchy);

        //TODO: Can we hardcode this?
        return (T)payload[0];
    }

    public TimeoutInfo Add<T>(T state, DateTimeOffset at)
    {
        var timeoutInfo = new TimeoutInfo
        {
            TimeoutId = Guid.NewGuid().ToString(),
            TriggerId = Guid.NewGuid().ToString(),
            ExactDueTime = at,
        };
        Serialize(state, timeoutInfo);
        timeoutInfos.Add(timeoutInfo);
        return timeoutInfo;
    }

    public TimeoutInfo Add<T>(T state, TimeSpan delay)
    {
        var timeoutInfo = new TimeoutInfo
        {
            TimeoutId = Guid.NewGuid().ToString(),
            TriggerId = Guid.NewGuid().ToString(),
            EstimatedDueTime = DateTimeOffset.UtcNow + delay
        };
        Serialize(state, timeoutInfo);
        timeoutInfos.Add(timeoutInfo);
        return timeoutInfo;
    }

    public void Remove(string timeoutId) => timeoutInfos.RemoveAll(x => x.TimeoutId == timeoutId);

    public IEnumerable<TimeoutReference<T>> Find<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(Saga saga, Func<TimeoutReference<T>, bool> predicate)
    {
        //HINT: We need Saga only to set the headers so it can be refactored away later
        return timeoutInfos
            .Where(x => x.Type == typeof(T).AssemblyQualifiedName)
            .Select(x => new TimeoutReference<T>(x, saga, this));
    }
}

/// <summary>
/// Represents a pending saga timeout
/// </summary>
public class TimeoutInfo
{
    /// <summary>
    /// Unique ID of this timeout
    /// </summary>
    public required string TimeoutId { get; set; }
    /// <summary>
    /// ID of the current trigger message
    /// </summary>
    public required string TriggerId { get; set; }
    /// <summary>
    /// Due time estimated based on the delay used to trigger the timeout
    /// </summary>
    public DateTimeOffset? EstimatedDueTime { get; set; }
    /// <summary>
    /// Due time requested
    /// </summary>
    public DateTimeOffset? ExactDueTime { get; set; }

    /// <summary>
    /// Full name of the type representing the timeout
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// The payload of the timeout as Base64 string
    /// </summary>
    public string Payload { get; set; }

    /// <summary>
    /// Content type to be used for selecting the deserialized for the payload
    /// </summary>
    public string ContentType { get; set; }
}
