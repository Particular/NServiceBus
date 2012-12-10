using MyMessages;
using NServiceBus;

namespace OrderService.MessageHandlers
{
    public class CreateMediumThumbnail : IHandleMessages<ImageUploaded>
    {
        private readonly IBus bus;

        public CreateMediumThumbnail(IBus bus)
        {
            this.bus = bus;
        }

        public void Handle(ImageUploaded message)
        {
            var thumb = new ThumbNailCreator().CreateThumbnail(message.Image.Value, 100, 100);

            var uri = new ThumbNailStore().Store(thumb, "medium-" + message.FileName, message.ContentType);

            var response = bus.CreateInstance<ThumbNailCreated>(x =>{ x.ThumbNailUrl = uri; x.Size = Size.Medium;});
            bus.Reply(response);
        }
    }
}