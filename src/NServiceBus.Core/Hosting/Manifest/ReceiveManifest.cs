#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;

record ReceiveManifest
{
    internal record HandledMessage
    {
        public required Type MessageType { get; set; }
        public required bool IsMessage { get; set; }
        public required bool IsCommand { get; set; }
        public required bool IsEvent { get; set; }
    }

    public required HandledMessage[] HandledMessages { get; init; }

    public required string[] EventTypes { get; init; }

    public IEnumerable<KeyValuePair<string, ManifestItem>> ToMessageManifest()
    {
        return [new("messageTypes",
            new ManifestItem
            {
                ArrayValue = HandledMessages.Select(
                    handledMessage => new ManifestItem { ItemValue = [
                        new("name", new ManifestItem { StringValue = handledMessage.MessageType.Name }),
                        new("fullName", new ManifestItem { StringValue = handledMessage.MessageType.FullName }),
                        new("isMessage", new ManifestItem { StringValue = handledMessage.IsMessage.ToString() }),
                        new("isEvent", new ManifestItem { StringValue = handledMessage.IsEvent.ToString() }),
                        new("isCommand", new ManifestItem { StringValue = handledMessage.IsCommand.ToString() }),
                        new("schema", new ManifestItem { ArrayValue = handledMessage.MessageType.GetProperties().Select(
                            prop => new ManifestItem { ItemValue = [
                                new("name", prop.Name),
                                new("type", prop.PropertyType.Name)
                                ]
                            }).ToArray() })
                        ] }).ToArray()
            }
        )];
    }
}
