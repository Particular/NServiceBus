using MyMessages;
using NServiceBus;

namespace OrderService.MessageHandlers
{
    public class CreateLargeThumbnail : IHandleMessages<ImageUploaded>
    {
        private readonly IBus bus;

        public CreateLargeThumbnail(IBus bus)
        {
            this.bus = bus;
        }

        public void Handle(ImageUploaded message)
        {
            var thumb = new ThumbNailCreator().CreateThumbnail(message.Image.Value, 200, 200);

            var uri = new ThumbNailStore().Store(thumb, "large-" + message.FileName, message.ContentType);

            var response = bus.CreateInstance<ThumbNailCreated>(x => { x.ThumbNailUrl = uri; x.Size = Size.Large; });
            bus.Reply(response);
        }
    }
}

