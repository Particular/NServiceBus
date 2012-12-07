using System.Drawing;
using System.IO;

namespace OrderService.MessageHandlers
{
    public class ThumbNailCreator
    {
        public Bitmap CreateThumbnail(byte[] bytes, int width, int height)
        {

            Bitmap thumbNail;
            try
            {
                var bmp = new Bitmap(new MemoryStream(bytes));

                decimal ratio;
                int newWidth;
                int newHeight;

                if (bmp.Width < width && bmp.Height < height)
                    return bmp;


                if (bmp.Width > bmp.Height)
                {
                    ratio = (decimal)width / bmp.Width;
                    newWidth = width;
                    var lnTemp = bmp.Height * ratio;
                    newHeight = (int)lnTemp;
                }
                else
                {
                    ratio = (decimal)height / bmp.Height;
                    newHeight = height;
                    var lnTemp = bmp.Width * ratio;
                    newWidth = (int)lnTemp;
                }

                thumbNail = new Bitmap(newWidth, newHeight);
                var g = Graphics.FromImage(thumbNail);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.FillRectangle(Brushes.White, 0, 0, newWidth, newHeight);
                g.DrawImage(bmp, 0, 0, newWidth, newHeight);

                bmp.Dispose();
            }
            catch
            {
                return null;
            }

            return thumbNail;
        }
    }
}