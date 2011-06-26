using System;
using System.Drawing.Imaging;
using MyMessages;
using NServiceBus;

namespace OrderWebSite
{
    public partial class _Default : System.Web.UI.Page, IHandleMessages<ThumbNailCreated>
    {
        private static string state;

        protected void Page_PreRender(object sender, EventArgs e)
        {
            status.InnerHtml = state;
        }

        protected void btnUpload_Click(object sender, EventArgs e)
        {
            Global.Bus
                .Send(new ImageUploaded
                {
                    Id = Guid.NewGuid(),
                    Image = new DataBusProperty<byte[]>(fileUpload.FileBytes),
                    FileName = fileUpload.FileName,
                    ContentType = fileUpload.PostedFile.ContentType
                });
            
           
            state = "Creating thumbnail...";
        }

        public void Handle(ThumbNailCreated message)
        {
            if (state == "Creating thumbnail...") state = "";

            state += "<img src=\"" + message.ThumbNailUrl + "\" border=\"0\" />";
        }
    }
}
