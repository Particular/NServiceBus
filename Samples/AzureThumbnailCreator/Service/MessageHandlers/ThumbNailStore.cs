using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;

namespace OrderService.MessageHandlers
{
    public class ThumbNailStore
    {
        public string Store(Bitmap thumb, string fileName, string  contentType)
        {
            var blobClient = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("ThumbNailStore.ConnectionString")).CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("images");
            container.CreateIfNotExist();
            container.SetPermissions( new BlobContainerPermissions{ PublicAccess = BlobContainerPublicAccessType.Container });

            var blob = container.GetBlockBlobReference(fileName);

            using(var stream = new MemoryStream())
            {
                thumb.Save(stream, DetermineImageFormat(contentType));
                stream.Seek(0, SeekOrigin.Begin);
                blob.UploadFromStream(stream);
            }

            return blob.Uri.ToString();
        }


        private ImageFormat DetermineImageFormat(string contentType)
        {
            if (contentType == "image/jpg")
            {
                return ImageFormat.Jpeg;
            }

            return ImageFormat.Jpeg;
        }

    }
}