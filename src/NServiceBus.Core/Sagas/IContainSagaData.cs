#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Sagas;
using Serialization;

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
    readonly IContainSagaTimeoutData sagaData;

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

    internal TimeoutReference(TimeoutInfo info, Saga saga, IContainSagaTimeoutData sagaData)
    {
        this.info = info;
        this.saga = saga;
        this.sagaData = sagaData;

        State = Deserialize(info.Payload);
    }

    /// <summary>
    /// Cancels this timeout.
    /// </summary>
    public void Cancel() => sagaData.TimeoutData.RemoveAll(x => x.TimeoutId == TimeoutId);

    /// <summary>
    /// Changes the due date of this timeout.
    /// </summary>
    /// <param name="newDueDate"></param>
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
        info.Payload = Serialize(State);

        return context.Send<TTimeoutMessageType>(State, options);
    }
}

class TimeoutData
{
    readonly List<TimeoutInfo> timeoutInfos;
    readonly MessageDeserializerResolver deserializerResolver;
    readonly IMessageSerializer mainSerializer;

    public TimeoutData(List<TimeoutInfo> timeoutInfos, MessageDeserializerResolver deserializerResolver, IMessageSerializer mainSerializer)
    {
        this.timeoutInfos = timeoutInfos;
        this.deserializerResolver = deserializerResolver;
        this.mainSerializer = mainSerializer;
    }

    public TimeoutInfo Add<T>(T state, DateTimeOffset at)
    {
        var timeoutInfo = new TimeoutInfo
        {
            TimeoutId = Guid.NewGuid().ToString(),
            TriggerId = Guid.NewGuid().ToString(),
            ExactDueTime = at,
            Type = typeof(T).FullName!,
            Payload = mainSerializer.Serialize(state,)
        };
        timeoutInfos.Add(timeoutInfo);
        return timeoutInfo;
    }

    public TimeoutInfo Add<T>(T state, TimeSpan delay)
    {
        var timeoutInfo = new TimeoutInfo
        {
            TimeoutId = Guid.NewGuid().ToString(),
            TriggerId = Guid.NewGuid().ToString(),
            EstimatedDueTime = DateTimeOffset.UtcNow + delay,
            Type = typeof(T).FullName!,
            Payload = mainSerializer.Serialize(state,)
        };
        timeoutInfos.Add(timeoutInfo);
        return timeoutInfo;
    }

    public IEnumerable<TimeoutReference<T>> Find<T>(Func<TimeoutReference<T>, bool> predicate)
    {
        return timeoutInfos
            .Where(x => x.Type == typeof(T).FullName)
            .Select(x => new TimeoutReference<T>(x,  ))
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
