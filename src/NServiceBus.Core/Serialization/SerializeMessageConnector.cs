namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using MessageMutator;
    using OutgoingPipeline;
    using Pipeline;
    using Pipeline.Contexts;
    using Serialization;
    using TransportDispatch;
    using Unicast.Messages;

    //todo: rename to LogicalOutgoingContext
    class SerializeMessageConnector : StageConnector<OutgoingLogicalMessageContext, OutgoingPhysicalMessageContext>
    {
        public SerializeMessageConnector(IMessageSerializer messageSerializer, MessageMetadataRegistry messageMetadataRegistry)
        {
            this.messageSerializer = messageSerializer;
            this.messageMetadataRegistry = messageMetadataRegistry;
        }

        public override async Task Invoke(OutgoingLogicalMessageContext context, Func<OutgoingPhysicalMessageContext, Task> next)
        {
            if (context.GetOrCreate<State>().SkipSerialization)
            {
                await next(new OutgoingPhysicalMessageContext(new byte[0], context)).ConfigureAwait(false);
                return;
            }
            context.SetHeader(Headers.ContentType, messageSerializer.ContentType);
            context.SetHeader(Headers.EnclosedMessageTypes, SerializeEnclosedMessageTypes(context.Message.MessageType));

            var streamMutators = InvokeMutators(context);
           
            var array = Serialize(context,streamMutators);

            await next(new OutgoingPhysicalMessageContext(array, context)).ConfigureAwait(false);
        }

         List<Func<Stream, Stream>> InvokeMutators(OutgoingLogicalMessageContext context)
        {
            var state = context.Get<OutgoingPhysicalToRoutingConnector.State>();
             var outgoingMessage = context.Message;

            InvokeHandlerContext incomingState;
            context.TryGetRootContext(out incomingState);

            object messageBeingHandled = null;
            Dictionary<string, string> incomingHeaders = null;
            if (incomingState != null)
            {
                messageBeingHandled = incomingState.MessageBeingHandled;
                incomingHeaders = incomingState.Headers;
            }

            var mutatorContext = new MutateOutgoingTransportMessageContext(outgoingMessage.Instance, state
                .Headers, messageBeingHandled, incomingHeaders);

            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingTransportMessages>())
            {
                mutator.MutateOutgoing(mutatorContext);
            }
             foreach (var modifiedHeader in mutatorContext.ModifiedHeaders)
             {
                context.SetHeader(modifiedHeader.Key,modifiedHeader.Value);
             }
             return mutatorContext.Decorators;
        }

        byte[] Serialize(OutgoingLogicalMessageContext context, List<Func<Stream, Stream>> decorators)
        {
             using (var ms = new MemoryStream())
            {
                Stream tempStream = ms;

                if (decorators.Any())
                {
                    tempStream = new PreserveBodyDecorator(tempStream);
                }
                foreach (var decorator in decorators)
                {
                    tempStream = decorator(tempStream);
                }
                messageSerializer.Serialize(context.Message.Instance, tempStream);
                return ms.ToArray();
            }
        }

        string SerializeEnclosedMessageTypes(Type messageType)
        {
            var metadata = messageMetadataRegistry.GetMessageMetadata(messageType);
            var distinctTypes = metadata.MessageHierarchy.Distinct();
            return string.Join(";", distinctTypes.Select(t => t.AssemblyQualifiedName));
        }

        public class State
        {
            public bool SkipSerialization { get; set; }
        }

        IMessageSerializer messageSerializer;
        MessageMetadataRegistry messageMetadataRegistry;
    }

    class PreserveBodyDecorator : Stream
    {
        readonly Stream tempStream;

        public PreserveBodyDecorator(Stream tempStream)
        {
            this.tempStream = tempStream;
        }

        public override void Flush()
        {
            tempStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return tempStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            tempStream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => true;

        public override bool CanSeek => tempStream.CanSeek;
        public override bool CanWrite => true;
        public override long Length => tempStream.Length;
        public override long Position
        {
            get { return tempStream.Position; }
            set { tempStream.Position = value; }
        }
    }

    /// <summary>
    /// Allows users to control serialization.
    /// </summary>
    public static class SerializationContextExtensions
    {
        /// <summary>
        /// Requests the serializer to skip serializing the message.
        /// </summary>
        public static void SkipSerialization(this OutgoingLogicalMessageContext context)
        {
            context.GetOrCreate<SerializeMessageConnector.State>().SkipSerialization = true;
        }
    }
}