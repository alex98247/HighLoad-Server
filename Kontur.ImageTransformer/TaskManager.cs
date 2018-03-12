using System;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace Kontur.ImageTransformer
{
    class TaskManager
    {
        const int maxTask = 40;
        private int countTasks = 0;

        public TaskManager()
        {

        }
        public void Add(HttpListenerContext listenerContext)
        {
            if (countTasks < maxTask && listenerContext.Request.Url.Segments[1] == "process/")
            {
                countTasks++;
                Task.Run(() => MethodProcess(listenerContext));
            }
            else
            {
                listenerContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                listenerContext.Response.Close();
            }
        }

        private async Task MethodProcess(HttpListenerContext listenerContext)
        {
            string method = listenerContext.Request.Url.Segments[2].Replace("/", "");
            int[] image_data = listenerContext.Request.Url.Segments[3].Replace("/", "").Split(',').Select(n => Convert.ToInt32(n)).ToArray();
            listenerContext.Response.StatusCode = (int)HttpStatusCode.OK;

            if (image_data.Length != 4) listenerContext.Response.StatusCode = (int)HttpStatusCode.BadRequest; //Целостность 

            using (MemoryStream mStream = new MemoryStream())
            {
                Bitmap b = new Bitmap(listenerContext.Request.InputStream);
                if (b.Size.Height > 1000 || b.Size.Width > 1000 || (int)listenerContext.Request.ContentLength64 > 100 * 1024)
                    listenerContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                else
                {
                    switch (method)
                    {
                        case "rotate-cw":
                            RotateCW(b);
                            break;
                        case "rotate-ccw":
                            RotateCCW(b);
                            break;
                        case "flip-v":
                            FlipV(b);
                            break;
                        case "flip-h":
                            FlipH(b);
                            break;
                        default:
                            listenerContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            break;
                    }
                    b = Crop(b, new Rectangle(image_data[0], image_data[1], image_data[2], image_data[3]));
                    if (b != null)
                    {
                        b.Save(mStream, ImageFormat.Png);
                        mStream.WriteTo(listenerContext.Response.OutputStream);
                        listenerContext.Response.Close();
                    }
                    else
                    {
                        listenerContext.Response.StatusCode = (int)HttpStatusCode.NoContent;
                        listenerContext.Response.Close();
                    }
                }
            }
            countTasks--;
        }

        private Bitmap RotateCW(Bitmap bitmap)
        {
            bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
            return bitmap;
        }


        private Bitmap RotateCCW(Bitmap bitmap)
        {
            bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
            return bitmap;
        }

        private Bitmap FlipH(Bitmap bitmap)
        {
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
            return bitmap;
        }

        private Bitmap FlipV(Bitmap bitmap)
        {
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            return bitmap;
        }

        private Bitmap Crop(Bitmap bitmap, Rectangle selectedRectangle)
        {
            Rectangle bitmapRectangle = new Rectangle(new Point(0,0), bitmap.Size);
            if (selectedRectangle.Width < 0)
            {
                selectedRectangle.X += selectedRectangle.Width;
                selectedRectangle.Width = -selectedRectangle.Width;
            }
            if (selectedRectangle.Height < 0)
            {
                selectedRectangle.Y += selectedRectangle.Height;
                selectedRectangle.Height = -selectedRectangle.Height;
            }
            Rectangle intersectedRectangle = Rectangle.Intersect(bitmapRectangle, selectedRectangle);
            if (!intersectedRectangle.IsEmpty)
                bitmap = bitmap.Clone(intersectedRectangle, bitmap.PixelFormat);
            else
                bitmap = null;
            return bitmap;
        }
    }
}
