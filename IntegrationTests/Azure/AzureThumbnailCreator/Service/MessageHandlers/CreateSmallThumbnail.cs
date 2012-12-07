using MyMessages;
using NServiceBus;

namespace OrderService.MessageHandlers
{
    public class CreateSmallThumbnail : IHandleMessages<ImageUploaded>
    {
        private readonly IBus bus;

        public CreateSmallThumbnail(IBus bus)
        {
            this.bus = bus;
        }

        public void Handle(ImageUploaded message)
        {
            var thumb = new ThumbNailCreator().CreateThumbnail(message.Image.Value, 50, 50);

            var uri = new ThumbNailStore().Store(thumb, "small-" + message.FileName, message.ContentType);

            var response = bus.CreateInstance<ThumbNailCreated>(x => { x.ThumbNailUrl = uri; x.Size = Size.Small; });
            bus.Reply(response);
        }
    }
}

